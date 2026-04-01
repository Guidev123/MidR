<p align="center">
  <a href="https://dotnet.microsoft.com/" target="_blank">
    <img src="https://upload.wikimedia.org/wikipedia/commons/e/ee/.NET_Core_Logo.svg" width="120" alt=".NET Logo" />
  </a>
</p>

<h1 align="center">MidR</h1>

<p align="center">
  <!-- MidR main package -->
  <a href="https://www.nuget.org/packages/MidR">
    <img class="badge" src="https://img.shields.io/nuget/v/MidR?color=purple&label=MidR" alt="MidR NuGet version" />
  </a>
  <a href="https://www.nuget.org/packages/MidR">
    <img class="badge" src="https://img.shields.io/nuget/dt/MidR?color=blue" alt="MidR NuGet downloads" />
  </a>


</p>


# MidR

A lightweight Mediator library for .NET with built-in support for request/response, in-process notifications, an async in-memory bus, and composable behavior pipelines.

## Installation

```bash
dotnet add package MidR
```

## Registration

Call `AddMidR` on your `IServiceCollection`. Pass the assemblies that contain your handlers — if you omit them, MidR scans all loaded assemblies automatically.

```csharp
// Explicit assembly
builder.Services.AddMidR(args: Assembly.GetExecutingAssembly());

// Multiple assemblies
builder.Services.AddMidR(args: typeof(CreateOrderHandler).Assembly, typeof(UserHandler).Assembly);

// Auto-scan (no args)
builder.Services.AddMidR();
```

### Concurrency limit for the async bus

The optional `maxConcurrency` parameter controls how many notifications the background dispatcher can have in-flight concurrently — i.e., how many are simultaneously awaiting I/O inside their handlers. Defaults to `Environment.ProcessorCount`.

```csharp
builder.Services.AddMidR(maxConcurrency: 4, args: Assembly.GetExecutingAssembly());
```

> The bus dispatches notifications concurrently, not in parallel. Each notification awaits its handlers cooperatively via `Task.WhenAll`, so the limit is about controlling pressure on downstream dependencies (database, HTTP, etc.) rather than CPU usage. Tune this value based on the connection pool size of your heaviest dependency rather than the number of cores.

\---

## Request / Response

### Defining a request

Implement `IRequest<TResponse>` for requests that return a value, or `IRequest` for void semantics (returns `Unit`).

```csharp
public sealed record CreateOrderRequest(Guid UserId, string Product) : IRequest<CreateOrderResponse>;
public sealed record CreateOrderResponse(Guid Id);

// Void — use IRequest (backed by Unit)
public sealed record DeleteOrderRequest(Guid Id) : IRequest;
```

### Implementing a handler

```csharp
public sealed class CreateOrderHandler : IRequestHandler<CreateOrderRequest, CreateOrderResponse>
{
    public async Task<CreateOrderResponse> ExecuteAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        // ...
        return new CreateOrderResponse(Guid.NewGuid());
    }
}
```

### Sending a request

Inject `IMediator` or `ISender` and call `SendAsync`.

```csharp
public class OrdersController(IMediator mediator)
{
    public async Task<IActionResult> Create(CreateOrderRequest request)
    {
        var result = await mediator.SendAsync(request);
        return Ok(result);
    }
}
```

\---

## Notifications

Notifications decouple the publisher from one or more handlers. MidR supports two dispatch strategies.

### Defining a notification

```csharp
public sealed record OrderCreatedEvent(Guid OrderId, Guid UserId) : INotification;
```

### Implementing handlers

Multiple handlers can be registered for the same notification — all of them will be invoked.

```csharp
public sealed class ReserveStockHandler : INotificationHandler<OrderCreatedEvent>
{
    public async Task ExecuteAsync(OrderCreatedEvent notification, CancellationToken cancellationToken)
    {
        // reserve stock...
    }
}

public sealed class SendConfirmationEmailHandler : INotificationHandler<OrderCreatedEvent>
{
    public async Task ExecuteAsync(OrderCreatedEvent notification, CancellationToken cancellationToken)
    {
        // send email...
    }
}
```

### Publishing

Inject `IMediator` or `IPublisher`.

#### `PublishAsync` — synchronous, in-process

Dispatches to all handlers sequentially and awaits their completion before returning. Use this when the caller must guarantee that all handlers have finished before continuing.

```csharp
await publisher.PublishAsync(new OrderCreatedEvent(orderId, userId));
// all handlers have completed at this point
```

#### `PublishToBusAsync` — asynchronous, fire-and-forget

Enqueues the notification in the in-memory `Channel`. A background `BackgroundService` dequeues and dispatches each notification's handlers concurrently via `Task.WhenAll`, independent of the caller's lifecycle.

```csharp
await publisher.PublishToBusAsync(new OrderCreatedEvent(orderId, userId));
// returns immediately; handlers run in the background
```

> Use `PublishToBusAsync` when the calling request should not wait for side effects (emails, audit logs, enrichment jobs). Use `PublishAsync` when consistency between the main operation and its side effects is required.

\---

## Behaviors

Behaviors form an ordered pipeline that wraps handler execution, similar to middleware. They are ideal for cross-cutting concerns such as logging, timing, validation, and exception handling.

### Request behaviors

Implement `IRequestBehavior<TRequest, TResponse>`. Must be an **open generic type** to be automatically applied to all matching requests.

```csharp
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IRequestBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> ExecuteAsync(
        TRequest request,
        RequestDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling {Request}", typeof(TRequest).Name);
        var response = await next();
        logger.LogInformation("Handled {Request}", typeof(TRequest).Name);
        return response;
    }
}
```

### Notification behaviors

Implement `INotificationBehavior<TNotification>`. Also must be an **open generic type**.

```csharp
public sealed class NotificationLoggingBehavior<TNotification>(ILogger<NotificationLoggingBehavior<TNotification>> logger)
    : INotificationBehavior<TNotification>
    where TNotification : INotification
{
    public async Task ExecuteAsync(
        TNotification notification,
        NotificationDelegate next,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Dispatching {Notification}", typeof(TNotification).Name);
        await next();
        logger.LogInformation("Dispatched {Notification}", typeof(TNotification).Name);
    }
}
```

### Registering behaviors

Chain `.WithBehaviors` after `AddMidR`. The `priority` value controls execution order — lower values run first (outermost in the pipeline).

```csharp
builder.Services.AddMidR(args: Assembly.GetExecutingAssembly())
    .WithBehaviors(config =>
    {
        // Request behaviors
        config.AddBehavior(typeof(LoggingBehavior<,>)).WithPriority(1);
        config.AddBehavior(typeof(TimingBehavior<,>)).WithPriority(2);
        config.AddBehavior(typeof(ExceptionBehavior<,>)).WithPriority(3);

        // Notification behaviors
        config.AddBehavior(typeof(NotificationLoggingBehavior<>)).WithPriority(1);
    });
```

> \*\*Important:\*\* always pass the open generic definition — `typeof(MyBehavior<,>)` for request behaviors and `typeof(MyBehavior<>)` for notification behaviors. MidR closes the type for each discovered request/notification at startup.

### Pipeline execution order

Given the registration above, a request flows through the pipeline as follows:

```
LoggingBehavior (P1)
  └─ TimingBehavior (P2)
       └─ ExceptionBehavior (P3)
            └─ Handler
```

Not calling `next()` inside a behavior short-circuits the pipeline — subsequent behaviors and the handler will not execute.

\---

## `Unit` — void requests

`Unit` is the return type for requests that produce no meaningful value. It avoids the need for a non-generic `IRequest` variant while keeping the pipeline uniform.

```csharp
public sealed record DeleteUserRequest(Guid Id) : IRequest<Unit>;

public sealed class DeleteUserHandler : IRequestHandler<DeleteUserRequest, Unit>
{
    public async Task<Unit> ExecuteAsync(DeleteUserRequest request, CancellationToken cancellationToken)
    {
        // delete user...
        return Unit.Value;
    }
}
```

\---

## Interface reference

|Interface|Purpose|
|-|-|
|`IRequest<TResponse>`|Marks a request that returns `TResponse`|
|`IRequest`|Marks a void request (returns `Unit`)|
|`IRequestHandler<TRequest, TResponse>`|Handles a specific request type|
|`INotification`|Marks a notification message|
|`INotificationHandler<TNotification>`|Handles a specific notification type|
|`IMediator`|Combined `ISender` + `IPublisher` entry point|
|`ISender`|Sends requests via `SendAsync`|
|`IPublisher`|Publishes notifications via `PublishAsync` / `PublishToBusAsync`|
|`IRequestBehavior<TRequest, TResponse>`|Pipeline behavior wrapping request handling|
|`INotificationBehavior<TNotification>`|Pipeline behavior wrapping notification dispatch|



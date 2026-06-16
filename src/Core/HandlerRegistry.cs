using Microsoft.Extensions.DependencyInjection;
using MidR.Abstractions;
using MidR.Behaviors;
using MidR.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MidR.Core
{
    internal sealed class HandlerRegistry
    {
        private readonly Dictionary<Type, Func<IServiceProvider, object, CancellationToken, Task<object>>> _handlers = new();
        private readonly Dictionary<Type, List<BehaviorExecutor>> _behaviors = new();
        private readonly Dictionary<Type, List<NotificationBehaviorExecutor>> _notificationBehaviors = new();
        private readonly Dictionary<Type, Func<IServiceProvider, object, CancellationToken, Task>> _notificationSequentialExecutors = new();
        private readonly Dictionary<Type, Func<IServiceProvider, object, CancellationToken, Task>> _notificationConcurrentExecutors = new();
        private readonly Dictionary<DirectHandlerKey, Func<IServiceProvider, object, CancellationToken, Task>> _directNotificationExecutors = new();
        private readonly Dictionary<DirectHandlerKey, Type> _directNotificationHandlerTypes = new();
        private static readonly BehaviorExecutor[] _emptyBehaviorsArray = Array.Empty<BehaviorExecutor>();
        private static readonly NotificationBehaviorExecutor[] _emptyNotificationBehaviorArray = Array.Empty<NotificationBehaviorExecutor>();
        private readonly Dictionary<Type, BehaviorExecutor[]> _behaviorsArrayCache = new();
        private readonly Dictionary<Type, NotificationBehaviorExecutor[]> _notificationBehaviorArrayCache = new();

        public void RegisterHandler<TRequest, TResponse>()
            where TRequest : class, IRequest<TResponse>
        {
            _handlers[typeof(TRequest)] = async (sp, request, ct) =>
            {
                var handler = sp.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
                var result = await handler.ExecuteAsync((TRequest)request, ct);
                return result!;
            };
        }

        public void RegisterNotificationHandler<TNotification>()
            where TNotification : class, INotification
        {
            _notificationSequentialExecutors[typeof(TNotification)] = async (sp, notification, ct) =>
            {
                var handlers = sp.GetServices<INotificationHandler<TNotification>>();
                foreach (var handler in handlers)
                {
                    await handler.ExecuteAsync((TNotification)notification, ct);
                }
            };

            _notificationConcurrentExecutors[typeof(TNotification)] = async (sp, notification, ct) =>
            {
                var handlers = sp.GetServices<INotificationHandler<TNotification>>();
                var tasks = new List<Task>();
                foreach (var handler in handlers)
                {
                    tasks.Add(handler.ExecuteAsync((TNotification)notification, ct));
                }

                await Task.WhenAll(tasks);
            };
        }

        public async Task ExecuteNotificationHandlersAsync(INotification notification, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            var notificationType = notification.GetType();

            if (!_notificationSequentialExecutors.TryGetValue(notificationType, out var executor))
            {
                return;
            }

            await executor(serviceProvider, notification, cancellationToken);
        }

        public async Task ExecuteNotificationHandlersConcurrentAsync(INotification notification, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            var notificationType = notification.GetType();

            if (!_notificationConcurrentExecutors.TryGetValue(notificationType, out var executor))
            {
                return;
            }

            await executor(serviceProvider, notification, cancellationToken);
        }

        public void RegisterDirectNotificationHandler<TNotification>(RoutingKey key, Type concreteHandlerType)
            where TNotification : class, INotification
        {
            var directKey = new DirectHandlerKey(typeof(TNotification), key);

            if (_directNotificationHandlerTypes.TryGetValue(directKey, out var existing))
            {
                throw new InvalidOperationException(
                    $"Duplicate direct routing key. Both '{existing.FullName}' and '{concreteHandlerType.FullName}' " +
                    $"are registered for notification '{typeof(TNotification).FullName}' with routing key '{key}'. " +
                    "A direct routing key must map to exactly one handler.");
            }

            _directNotificationHandlerTypes[directKey] = concreteHandlerType;
            _directNotificationExecutors[directKey] = (sp, notification, ct) =>
            {
                var handler = (INotificationHandler<TNotification>)sp.GetRequiredService(concreteHandlerType);
                return handler.ExecuteAsync((TNotification)notification, ct);
            };
        }

        public Task ExecuteDirectNotificationHandlerAsync(INotification notification, RoutingKey key, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            var directKey = new DirectHandlerKey(notification.GetType(), key);

            if (!_directNotificationExecutors.TryGetValue(directKey, out var executor))
            {
                throw new InvalidOperationException(
                    $"No direct handler registered for notification '{notification.GetType().FullName}' " +
                    $"with routing key '{key}'. Decorate the target handler with [DirectQueue(\"{key}\")].");
            }

            return executor(serviceProvider, notification, cancellationToken);
        }

        public void RegisterBehavior<TRequest, TResponse>(Type concreteBehaviorType, int priority)
            where TRequest : class, IRequest<TResponse>
        {
            var requestType = typeof(TRequest);

            if (!_behaviors.ContainsKey(requestType))
                _behaviors[requestType] = new List<BehaviorExecutor>();

            var executor = new BehaviorExecutor<TRequest, TResponse>(concreteBehaviorType, priority);
            _behaviors[requestType].Add(executor);

            _behaviors[requestType] = _behaviors[requestType].OrderBy(b => b.Priority).ToList();
        }

        public void RegisterNotificationBehavior<TNotification>(Type concreteBehaviorType, int priority)
            where TNotification : class, INotification
        {
            var notificationType = typeof(TNotification);

            if (!_notificationBehaviors.ContainsKey(notificationType))
            {
                _notificationBehaviors[notificationType] = new List<NotificationBehaviorExecutor>();
            }

            var executor = new NotificationBehaviorExecutor<TNotification>(concreteBehaviorType, priority);
            _notificationBehaviors[notificationType].Add(executor);

            _notificationBehaviors[notificationType] = _notificationBehaviors[notificationType].OrderBy(b => b.Priority).ToList();
        }

        public async Task<TResponse> ExecuteHandlerAsync<TResponse>(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var requestType = request.GetType();

            if (!_handlers.TryGetValue(requestType, out var handlerExecutor))
                throw new InvalidOperationException($"Handler not found for {requestType.Name}");

            var result = await handlerExecutor(serviceProvider, request, cancellationToken);
            return (TResponse)result;
        }

        public BehaviorExecutor[] GetBehaviorsArray(Type requestType)
        {
            if (_behaviorsArrayCache.TryGetValue(requestType, out var cached))
            {
                return cached;
            }

            if (!_behaviors.TryGetValue(requestType, out var behaviors) || behaviors.Count == 0)
            {
                _behaviorsArrayCache[requestType] = _emptyBehaviorsArray;
                return _emptyBehaviorsArray;
            }

            var array = behaviors.ToArray();
            _behaviorsArrayCache[requestType] = array;
            return array;
        }

        public NotificationBehaviorExecutor[] GetNotificationBehaviorsArray(Type notificationType)
        {
            if (_notificationBehaviorArrayCache.TryGetValue(notificationType, out var cached))
            {
                return cached;
            }

            if (!_notificationBehaviors.TryGetValue(notificationType, out var behaviors) || behaviors.Count == 0)
            {
                _notificationBehaviorArrayCache[notificationType] = _emptyNotificationBehaviorArray;
                return _emptyNotificationBehaviorArray;
            }

            var array = behaviors.ToArray();
            _notificationBehaviorArrayCache[notificationType] = array;
            return array;
        }

        public bool HasHandler(Type requestType) => _handlers.ContainsKey(requestType);
    }
}
using MidR.Abstractions;
using MidR.Core;
using MidR.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MidR.Behaviors
{
    internal sealed class NotificationBehaviorPipeline : INotificationBehaviorPipeline
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly HandlerRegistry _handlerRegistry;

        public NotificationBehaviorPipeline(IServiceProvider serviceProvider, HandlerRegistry handlerRegistry)
        {
            _serviceProvider = serviceProvider;
            _handlerRegistry = handlerRegistry;
        }

        public Task ExecuteAsync(INotification notification, CancellationToken cancellationToken = default)
        {
            return RunAsync(
                notification,
                (n, sp, ct) => _handlerRegistry.ExecuteNotificationHandlersAsync(n, sp, ct),
                cancellationToken);
        }

        public Task ExecuteConcurrentAsync(INotification notification, CancellationToken cancellationToken = default)
        {
            return RunAsync(
                notification,
                (n, sp, ct) => _handlerRegistry.ExecuteNotificationHandlersConcurrentAsync(n, sp, ct),
                cancellationToken);
        }

        public Task ExecuteDirectAsync(INotification notification, RoutingKey routingKey, CancellationToken cancellationToken = default)
        {
            return RunAsync(
                notification,
                (n, sp, ct) => _handlerRegistry.ExecuteDirectNotificationHandlerAsync(n, routingKey, sp, ct),
                cancellationToken);
        }

        private Task RunAsync(
            INotification notification,
            Func<INotification, IServiceProvider, CancellationToken, Task> terminal,
            CancellationToken cancellationToken)
        {
            var behaviors = _handlerRegistry.GetNotificationBehaviorsArray(notification.GetType());

            if (behaviors.Length == 0)
            {
                return terminal(notification, _serviceProvider, cancellationToken);
            }

            return ExecutePipelineAsync(notification, behaviors, 0, _serviceProvider, terminal, cancellationToken);
        }

        private Task ExecutePipelineAsync(
            INotification notification,
            NotificationBehaviorExecutor[] behaviors,
            int index,
            IServiceProvider serviceProvider,
            Func<INotification, IServiceProvider, CancellationToken, Task> terminal,
            CancellationToken cancellationToken)
        {
            if (index >= behaviors.Length)
            {
                return terminal(notification, serviceProvider, cancellationToken);
            }

            var behavior = behaviors[index];
            return behavior.ExecuteTypedAsync(
                notification,
                () => ExecutePipelineAsync(notification, behaviors, index + 1, serviceProvider, terminal, cancellationToken),
                serviceProvider,
                cancellationToken);
        }
    }
}
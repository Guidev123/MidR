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
            var notificationType = notification.GetType();
            var behaviors = _handlerRegistry.GetNotificationBehaviorsArray(notificationType);

            if (behaviors.Length == 0)
            {
                return _handlerRegistry.ExecuteNotificationHandlersAsync(notification, _serviceProvider, cancellationToken);
            }

            return ExecutePipelineAsync(notification, behaviors, 0, _serviceProvider, cancellationToken, isConcurrent: false);
        }

        public Task ExecuteConcurrentAsync(INotification notification, CancellationToken cancellationToken = default)
        {
            var notificationType = notification.GetType();
            var behaviors = _handlerRegistry.GetNotificationBehaviorsArray(notificationType);

            if (behaviors.Length == 0)
            {
                return _handlerRegistry.ExecuteNotificationHandlersConcurrentAsync(notification, _serviceProvider, cancellationToken);
            }

            return ExecutePipelineAsync(notification, behaviors, 0, _serviceProvider, cancellationToken, isConcurrent: true);
        }

        private Task ExecutePipelineAsync(
            INotification notification,
            NotificationBehaviorExecutor[] behaviors,
            int index,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken,
            bool isConcurrent)
        {
            if (index >= behaviors.Length)
            {
                return isConcurrent
                    ? _handlerRegistry.ExecuteNotificationHandlersConcurrentAsync(notification, serviceProvider, cancellationToken)
                    : _handlerRegistry.ExecuteNotificationHandlersAsync(notification, serviceProvider, cancellationToken);
            }

            var behavior = behaviors[index];
            return behavior.ExecuteTypedAsync(
                notification,
                () => ExecutePipelineAsync(notification, behaviors, index + 1, serviceProvider, cancellationToken, isConcurrent),
                serviceProvider,
                cancellationToken);
        }
    }
}
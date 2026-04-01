using MidR.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MidR.Behaviors
{
    internal abstract class NotificationBehaviorExecutor
    {
        public int Priority { get; }
        public Type ConcreteBehaviorType { get; }

        protected NotificationBehaviorExecutor(Type concreteBehaviorType, int priority)
        {
            ConcreteBehaviorType = concreteBehaviorType;
            Priority = priority;
        }

        public abstract Task ExecuteTypedAsync(object notification, Func<Task> next, IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }

    internal sealed class NotificationBehaviorExecutor<TNotification> : NotificationBehaviorExecutor
        where TNotification : class, INotification
    {
        private readonly Type _closedConcreteBehaviorType;

        public NotificationBehaviorExecutor(Type concreteBehaviorType, int priority) : base(concreteBehaviorType, priority)
        {
            _closedConcreteBehaviorType = concreteBehaviorType.IsGenericTypeDefinition
                ? concreteBehaviorType.MakeGenericType(typeof(TNotification))
                : concreteBehaviorType;
        }

        public override async Task ExecuteTypedAsync(object notification, Func<Task> next, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            if (serviceProvider.GetService(_closedConcreteBehaviorType) is not INotificationBehavior<TNotification> behavior)
            {
                await next();
                return;
            }

            await behavior.ExecuteAsync((TNotification)notification, async () =>
            {
                await next();
            }, cancellationToken);
        }
    }
}
using MidR.Abstractions;
using MidR.Behaviors;
using MidR.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MidR.Core
{
    internal sealed class Publisher : IPublisher
    {
        private readonly MemoryBus _bus;
        private readonly INotificationBehaviorPipeline _pipeline;

        public Publisher(MemoryBus bus, INotificationBehaviorPipeline pipeline)
        {
            _bus = bus;
            _pipeline = pipeline;
        }

        public Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        {
            return _pipeline.ExecuteAsync(notification, cancellationToken);
        }

        public Task PublishAsync<TNotification>(TNotification notification, RoutingKey routingKey, CancellationToken cancellationToken = default) where TNotification : INotification
        {
            return _pipeline.ExecuteDirectAsync(notification, routingKey, cancellationToken);
        }

        public Task PublishToBusAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        {
            return _bus.EnqueueAsync(notification, cancellationToken);
        }
    }
}
using MidR.Abstractions;
using MidR.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace MidR.Core
{
    internal sealed class Mediator : IMediator
    {
        private readonly ISender _sender;
        private readonly IPublisher _publisher;

        public Mediator(ISender sender, IPublisher publisher)
        {
            _sender = sender;
            _publisher = publisher;
        }

        public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            return _sender.SendAsync(request, cancellationToken);
        }

        public Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            return _publisher.PublishAsync(notification, cancellationToken);
        }

        public Task PublishAsync<TNotification>(TNotification notification, RoutingKey routingKey, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            return _publisher.PublishAsync(notification, routingKey, cancellationToken);
        }

        public Task PublishToBusAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        {
            return _publisher.PublishToBusAsync(notification, cancellationToken);
        }
    }
}
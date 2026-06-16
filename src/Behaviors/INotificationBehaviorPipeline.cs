using MidR.Abstractions;
using MidR.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace MidR.Behaviors
{
    internal interface INotificationBehaviorPipeline
    {
        Task ExecuteAsync(INotification notification, CancellationToken cancellationToken = default);

        Task ExecuteConcurrentAsync(INotification notification, CancellationToken cancellationToken = default);

        Task ExecuteDirectAsync(INotification notification, RoutingKey routingKey, CancellationToken cancellationToken = default);
    }
}
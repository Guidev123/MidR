using MidR.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace MidR.Interfaces
{
    /// <summary>
    /// Provides functionality to publish notifications to in-process handlers
    /// or in-memory message bus.
    /// </summary>
    public interface IPublisher
    {
        /// <summary>
        /// Publishes a notification to all in-process handlers.
        /// </summary>
        /// <typeparam name="TNotification">The type of notification.</typeparam>
        /// <param name="notification">The notification instance to publish.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        Task PublishAsync<TNotification>(
            TNotification notification,
            CancellationToken cancellationToken = default)
            where TNotification : INotification;

        /// <summary>
        /// Publishes a notification to the single in-process handler bound to the specified
        /// <paramref name="routingKey"/> via <see cref="DirectQueueAttribute"/> (Direct Dispatch).
        /// </summary>
        /// <typeparam name="TNotification">The type of notification.</typeparam>
        /// <param name="notification">The notification instance to publish.</param>
        /// <param name="routingKey">The routing key identifying the target handler.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <remarks>
        /// Only the handler registered under the pair (<typeparamref name="TNotification"/>,
        /// <paramref name="routingKey"/>) is invoked. Throws if no handler is registered for the key.
        /// </remarks>
        Task PublishAsync<TNotification>(
            TNotification notification,
            RoutingKey routingKey,
            CancellationToken cancellationToken = default)
            where TNotification : INotification;

        /// <summary>
        /// Publishes a notification to an in-memory message bus.
        /// </summary>
        /// <typeparam name="TNotification">The type of notification.</typeparam>
        /// <param name="notification">The notification instance to publish.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        Task PublishToBusAsync<TNotification>(
            TNotification notification,
            CancellationToken cancellationToken = default)
            where TNotification : INotification;
    }
}
using System.Threading;
using System.Threading.Tasks;

namespace MidR.Interfaces
{
    /// <summary>
    /// Defines a handler for processing a specific type of notification.
    /// </summary>
    /// <typeparam name="TNotification">
    /// The type of the notification that this handler processes. Must implement <see cref="INotification"/>.
    /// </typeparam>
    public interface INotificationHandler<in TNotification>
        where TNotification : INotification
    {
        /// <summary>
        /// Executes the handling logic for the specified notification asynchronously.
        /// </summary>
        /// <param name="notification">The notification instance to handle.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous execution of the notification handling logic.</returns>
        Task ExecuteAsync(TNotification notification, CancellationToken cancellationToken);
    }
}
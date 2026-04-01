using MidR.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace MidR.Behaviors
{
    /// <summary>
    /// Represents a pipeline behavior that can intercept and wrap notification handling logic.
    /// </summary>
    /// <remarks>
    /// Notification behaviors form an ordered pipeline that wraps the execution of all
    /// <see cref="INotificationHandler{TNotification}"/> registered for a given notification type.
    /// Each behavior receives a <see cref="NotificationDelegate"/> representing the remainder of
    /// the pipeline — invoking it advances execution to the next behavior, or to the handlers
    /// themselves when no further behaviors remain.
    /// <para>
    /// Implementations must be open generic types (e.g. <c>MyBehavior&lt;TNotification&gt;</c>)
    /// and registered via <c>AddBehavior(typeof(MyBehavior&lt;&gt;))</c> so the framework can
    /// close the type for each discovered notification at startup.
    /// </para>
    /// </remarks>
    /// <typeparam name="TNotification">
    /// The type of notification this behavior intercepts. Must implement <see cref="INotification"/>.
    /// </typeparam>
    public interface INotificationBehavior<TNotification> where TNotification : INotification
    {
        /// <summary>
        /// Executes the behavior logic around the next delegate in the notification pipeline.
        /// </summary>
        /// <param name="notification">The notification instance being dispatched.</param>
        /// <param name="next">
        /// The next delegate in the pipeline. Invoking this continues execution toward the
        /// remaining behaviors and, ultimately, the notification handlers. Not invoking it
        /// short-circuits the pipeline, preventing handlers from being called.
        /// </param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> that completes when the behavior finishes executing.</returns>
        Task ExecuteAsync(TNotification notification, NotificationDelegate next, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Represents the next delegate in the notification pipeline.
    /// </summary>
    /// <remarks>
    /// Invoking this delegate advances execution to the next registered
    /// <see cref="INotificationBehavior{TNotification}"/>, or directly to the notification
    /// handlers if no further behaviors remain in the pipeline.
    /// </remarks>
    /// <returns>A <see cref="Task"/> that completes when the remainder of the pipeline finishes.</returns>
    public delegate Task NotificationDelegate();
}
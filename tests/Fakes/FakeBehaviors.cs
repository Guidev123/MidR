using MidR.Behaviors;
using MidR.Interfaces;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MidR.UnitTests.Fakes
{
    public class FakeRequestBehavior<TRequest, TResponse> : IRequestBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public static ConcurrentBag<string> Calls { get; set; } = new();

        public async Task<TResponse> ExecuteAsync(TRequest request, RequestDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            Calls.Add("Before");
            var result = await next();
            Calls.Add("After");
            return result;
        }
    }

    public class SecondRequestBehavior<TRequest, TResponse> : IRequestBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public static ConcurrentBag<string> Calls { get; set; } = new();

        public async Task<TResponse> ExecuteAsync(TRequest request, RequestDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            Calls.Add("SecondBefore");
            var result = await next();
            Calls.Add("SecondAfter");
            return result;
        }
    }

    public class FakeNotificationBehavior<TNotification> : INotificationBehavior<TNotification>
        where TNotification : INotification
    {
        public static ConcurrentBag<string> Calls { get; set; } = new();

        public async Task ExecuteAsync(TNotification notification, NotificationDelegate next, CancellationToken cancellationToken)
        {
            Calls.Add("NotifBefore");
            await next();
            Calls.Add("NotifAfter");
        }
    }
}

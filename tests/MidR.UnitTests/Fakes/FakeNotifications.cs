using MidR.Interfaces;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MidR.UnitTests.Fakes
{
    public class FakeNotification : INotification
    {
        public string Message { get; set; } = string.Empty;
    }

    public class FakeNotificationHandlerA : INotificationHandler<FakeNotification>
    {
        public static ConcurrentBag<string> Calls { get; set; } = new();

        public Task ExecuteAsync(FakeNotification notification, CancellationToken cancellationToken)
        {
            Calls.Add($"A:{notification.Message}");
            return Task.CompletedTask;
        }
    }

    public class FakeNotificationHandlerB : INotificationHandler<FakeNotification>
    {
        public static ConcurrentBag<string> Calls { get; set; } = new();

        public Task ExecuteAsync(FakeNotification notification, CancellationToken cancellationToken)
        {
            Calls.Add($"B:{notification.Message}");
            return Task.CompletedTask;
        }
    }

    public class FakeNotificationWithNoHandlers : INotification
    {
    }
}

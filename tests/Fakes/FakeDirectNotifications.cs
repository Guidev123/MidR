using MidR.Abstractions;
using MidR.Interfaces;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MidR.UnitTests.Fakes
{
    public static class FakeDirectRoutes
    {
        public const string A = "module-a";
        public const string B = "module-b";
    }

    public class FakeDirectNotification : INotification
    {
        public string Message { get; set; } = string.Empty;
    }

    [DirectQueue(FakeDirectRoutes.A)]
    public class FakeDirectHandlerA : INotificationHandler<FakeDirectNotification>
    {
        public static ConcurrentBag<string> Calls { get; set; } = new();

        public Task ExecuteAsync(FakeDirectNotification notification, CancellationToken cancellationToken)
        {
            Calls.Add($"A:{notification.Message}");
            return Task.CompletedTask;
        }
    }

    [DirectQueue(FakeDirectRoutes.B)]
    public class FakeDirectHandlerB : INotificationHandler<FakeDirectNotification>
    {
        public static ConcurrentBag<string> Calls { get; set; } = new();

        public Task ExecuteAsync(FakeDirectNotification notification, CancellationToken cancellationToken)
        {
            Calls.Add($"B:{notification.Message}");
            return Task.CompletedTask;
        }
    }

    public class FakeDirectFanOutHandler : INotificationHandler<FakeDirectNotification>
    {
        public static ConcurrentBag<string> Calls { get; set; } = new();

        public Task ExecuteAsync(FakeDirectNotification notification, CancellationToken cancellationToken)
        {
            Calls.Add($"FanOut:{notification.Message}");
            return Task.CompletedTask;
        }
    }
}
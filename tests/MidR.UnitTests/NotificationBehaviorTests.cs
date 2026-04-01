using Microsoft.Extensions.DependencyInjection;
using MidR.DependencyInjection;
using MidR.Interfaces;
using MidR.UnitTests.Fakes;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MidR.UnitTests
{
    public class NotificationBehaviorTests
    {
        private IServiceProvider BuildProvider(Action<MidRConfiguration>? configure = null)
        {
            var services = new ServiceCollection();
            var config = services.AddMidR(0, typeof(NotificationBehaviorTests).Assembly);
            configure?.Invoke(config);
            return services.BuildServiceProvider();
        }

        [Fact]
        public async Task PublishAsync_WithBehavior_ExecutesBehaviorAroundHandlers()
        {
            FakeNotificationBehavior<FakeNotification>.Calls = new();
            FakeNotificationHandlerA.Calls = new();
            FakeNotificationHandlerB.Calls = new();

            var provider = BuildProvider(config =>
                config.WithBehaviors(b =>
                    b.AddBehavior(typeof(FakeNotificationBehavior<>)).WithPriority(0)));

            using var scope = provider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(new FakeNotification { Message = "msg" });

            Assert.Contains("NotifBefore", FakeNotificationBehavior<FakeNotification>.Calls);
            Assert.Contains("NotifAfter", FakeNotificationBehavior<FakeNotification>.Calls);
            Assert.Contains("A:msg", FakeNotificationHandlerA.Calls);
            Assert.Contains("B:msg", FakeNotificationHandlerB.Calls);
        }

        [Fact]
        public async Task PublishAsync_WithoutBehavior_HandlersStillExecute()
        {
            FakeNotificationHandlerA.Calls = new();
            FakeNotificationHandlerB.Calls = new();

            using var scope = BuildProvider().CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(new FakeNotification { Message = "direct" });

            Assert.Contains("A:direct", FakeNotificationHandlerA.Calls);
            Assert.Contains("B:direct", FakeNotificationHandlerB.Calls);
        }
    }
}

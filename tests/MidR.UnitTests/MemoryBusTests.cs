using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MidR.DependencyInjection;
using MidR.Interfaces;
using MidR.UnitTests.Fakes;

namespace MidR.UnitTests
{
    public class MemoryBusTests
    {
        [Fact]
        public async Task PublishToBusAsync_EventuallyExecutesHandlers()
        {
            FakeNotificationHandlerA.Calls = new();
            FakeNotificationHandlerB.Calls = new();

            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddMidR(1, typeof(MemoryBusTests).Assembly);
                })
                .Build();

            await host.StartAsync();

            using var scope = host.Services.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishToBusAsync(new FakeNotification { Message = "bus" });

            await Task.Delay(500);

            Assert.Contains("A:bus", FakeNotificationHandlerA.Calls);
            Assert.Contains("B:bus", FakeNotificationHandlerB.Calls);

            await host.StopAsync();
        }

        [Fact]
        public async Task PublishToBusAsync_WithBehavior_ExecutesBehavior()
        {
            FakeNotificationBehavior<FakeNotification>.Calls = new();
            FakeNotificationHandlerA.Calls = new();

            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddMidR(1, typeof(MemoryBusTests).Assembly)
                        .WithBehaviors(b =>
                            b.AddBehavior(typeof(FakeNotificationBehavior<>)).WithPriority(0));
                })
                .Build();

            await host.StartAsync();

            using var scope = host.Services.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishToBusAsync(new FakeNotification { Message = "busbhv" });

            await Task.Delay(500);

            Assert.Contains("NotifBefore", FakeNotificationBehavior<FakeNotification>.Calls);
            Assert.Contains("NotifAfter", FakeNotificationBehavior<FakeNotification>.Calls);
            Assert.Contains("A:busbhv", FakeNotificationHandlerA.Calls);

            await host.StopAsync();
        }
    }
}
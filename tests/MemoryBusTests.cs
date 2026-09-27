using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MidR.Core;
using MidR.DependencyInjection;
using MidR.Interfaces;
using MidR.UnitTests.Fakes;
using System.Threading.Channels;

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
                    services.AddMidR(typeof(MemoryBusTests).Assembly)
                        .WithMemoryBus(MemoryBusOptions.CreateUnbounded(maxConcurrency: 1));
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
                    services.AddMidR(typeof(MemoryBusTests).Assembly)
                        .WithBehaviors(b =>
                            b.AddBehavior(typeof(FakeNotificationBehavior<>)).WithPriority(0))
                        .WithMemoryBus(MemoryBusOptions.CreateUnbounded(maxConcurrency: 1));
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

        [Fact]
        public void CreateUnbounded_IsTheDefault_AndRespectsMaxConcurrency()
        {
            var bus = new MemoryBus(MemoryBusOptions.CreateUnbounded(maxConcurrency: 7));

            Assert.Equal(ChannelType.Unbounded, bus.Options.ChannelType);
            Assert.Equal(7, bus.Options.MaxConcurrency);
        }

        [Fact]
        public async Task Bounded_Channel_AppliesBackpressure_WhenFull()
        {
            var bus = new MemoryBus(MemoryBusOptions.CreateBounded(
                new BoundedChannelOptions(capacity: 1) { FullMode = BoundedChannelFullMode.Wait }));

            await bus.EnqueueAsync(new FakeNotification { Message = "first" }); // fills the single slot

            var secondEnqueue = bus.EnqueueAsync(new FakeNotification { Message = "second" });
            var completedBeforeAnyRead = await Task.WhenAny(secondEnqueue, Task.Delay(200)) == secondEnqueue;

            Assert.False(completedBeforeAnyRead, "a full bounded channel should block the write instead of completing immediately");

            await bus.Reader.ReadAsync(); // drain one slot
            await secondEnqueue; // now it should complete

            Assert.True(secondEnqueue.IsCompletedSuccessfully);
        }

        [Fact]
        public void Bounded_WithoutBoundedChannelOptions_ThrowsInsteadOfLeavingChannelNull()
        {
            var invalidOptions = MemoryBusOptions.CreateBounded(null!);

            Assert.Throws<ArgumentException>(() => new MemoryBus(invalidOptions));
        }
    }
}
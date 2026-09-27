using Microsoft.Extensions.DependencyInjection;
using MidR.Abstractions;
using MidR.Core;
using MidR.DependencyInjection;
using MidR.Interfaces;
using MidR.UnitTests.Fakes;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MidR.UnitTests
{
    public class DirectDispatchTests
    {
        private IServiceProvider BuildProvider(Action<MidRConfiguration>? configure = null)
        {
            var services = new ServiceCollection();
            var config = services.AddMidR(typeof(DirectDispatchTests).Assembly);
            configure?.Invoke(config);
            return services.BuildServiceProvider();
        }

        private static void ResetCalls()
        {
            FakeDirectHandlerA.Calls = new();
            FakeDirectHandlerB.Calls = new();
            FakeDirectFanOutHandler.Calls = new();
        }

        [Fact]
        public async Task PublishAsync_WithRoutingKey_InvokesOnlyMatchingHandler()
        {
            ResetCalls();

            using var scope = BuildProvider().CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(new FakeDirectNotification { Message = "hi" }, FakeDirectRoutes.A);

            Assert.Contains("A:hi", FakeDirectHandlerA.Calls);
            Assert.Empty(FakeDirectHandlerB.Calls);
            Assert.Empty(FakeDirectFanOutHandler.Calls);
        }

        [Fact]
        public async Task PublishAsync_WithRoutingKey_DoesNotInvokeOtherDirectHandlers()
        {
            ResetCalls();

            using var scope = BuildProvider().CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(new FakeDirectNotification { Message = "x" }, FakeDirectRoutes.B);

            Assert.Contains("B:x", FakeDirectHandlerB.Calls);
            Assert.Empty(FakeDirectHandlerA.Calls);
        }

        [Fact]
        public async Task PublishAsync_WithoutRoutingKey_DoesNotInvokeDirectHandlers()
        {
            ResetCalls();

            using var scope = BuildProvider().CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(new FakeDirectNotification { Message = "broadcast" });

            Assert.Empty(FakeDirectHandlerA.Calls);
            Assert.Empty(FakeDirectHandlerB.Calls);
        }

        [Fact]
        public async Task PublishAsync_WithoutRoutingKey_StillFanOutsNonDirectHandlers()
        {
            ResetCalls();

            using var scope = BuildProvider().CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(new FakeDirectNotification { Message = "broadcast" });

            Assert.Contains("FanOut:broadcast", FakeDirectFanOutHandler.Calls);
        }

        [Fact]
        public async Task PublishAsync_WithRoutingKey_UnknownKey_Throws()
        {
            ResetCalls();

            using var scope = BuildProvider().CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                publisher.PublishAsync(new FakeDirectNotification { Message = "x" }, "no-such-key"));
        }

        [Fact]
        public async Task PublishAsync_WithRoutingKey_RunsThroughBehaviorPipeline()
        {
            ResetCalls();
            FakeNotificationBehavior<FakeDirectNotification>.Calls = new();

            var provider = BuildProvider(config =>
                config.WithBehaviors(b =>
                    b.AddBehavior(typeof(FakeNotificationBehavior<>)).WithPriority(0)));

            using var scope = provider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(new FakeDirectNotification { Message = "m" }, FakeDirectRoutes.A);

            Assert.Contains("NotifBefore", FakeNotificationBehavior<FakeDirectNotification>.Calls);
            Assert.Contains("NotifAfter", FakeNotificationBehavior<FakeDirectNotification>.Calls);
            Assert.Contains("A:m", FakeDirectHandlerA.Calls);
        }

        [Fact]
        public void AddMidR_RegistersDirectHandler_NotAsFanOutInterface()
        {
            using var scope = BuildProvider().CreateScope();

            var handlers = scope.ServiceProvider
                .GetServices<INotificationHandler<FakeDirectNotification>>()
                .ToList();

            // The two direct handlers are excluded from the fan-out interface registration;
            // only the plain fan-out handler remains resolvable via the interface.
            Assert.Single(handlers);
            Assert.IsType<FakeDirectFanOutHandler>(handlers[0]);
        }

        [Fact]
        public void RegisterDirectNotificationHandler_DuplicateKey_ThrowsAtStartup()
        {
            var registry = new HandlerRegistry();
            RoutingKey key = "dup";

            registry.RegisterDirectNotificationHandler<FakeDirectNotification>(key, typeof(FakeDirectHandlerA));

            var ex = Assert.Throws<InvalidOperationException>(() =>
                registry.RegisterDirectNotificationHandler<FakeDirectNotification>(key, typeof(FakeDirectHandlerB)));

            Assert.Contains("Duplicate direct routing key", ex.Message);
        }
    }
}

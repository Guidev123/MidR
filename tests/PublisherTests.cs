using Microsoft.Extensions.DependencyInjection;
using MidR.DependencyInjection;
using MidR.Interfaces;
using MidR.UnitTests.Fakes;

namespace MidR.UnitTests
{
    public class PublisherTests
    {
        private IServiceProvider BuildProvider(Action<MidRConfiguration>? configure = null)
        {
            var services = new ServiceCollection();
            var config = services.AddMidR(typeof(PublisherTests).Assembly);
            configure?.Invoke(config);
            return services.BuildServiceProvider();
        }

        [Fact]
        public async Task PublishAsync_InvokesAllRegisteredHandlers()
        {
            FakeNotificationHandlerA.Calls = new();
            FakeNotificationHandlerB.Calls = new();

            using var scope = BuildProvider().CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(new FakeNotification { Message = "hello" });

            Assert.Contains("A:hello", FakeNotificationHandlerA.Calls);
            Assert.Contains("B:hello", FakeNotificationHandlerB.Calls);
        }

        [Fact]
        public async Task PublishAsync_NoHandlers_CompletesWithoutError()
        {
            using var scope = BuildProvider().CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(new FakeNotificationWithNoHandlers());
        }
    }
}
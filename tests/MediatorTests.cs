using Microsoft.Extensions.DependencyInjection;
using MidR.DependencyInjection;
using MidR.Interfaces;
using MidR.UnitTests.Fakes;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MidR.UnitTests
{
    public class MediatorTests
    {
        private IServiceProvider BuildProvider()
        {
            var services = new ServiceCollection();
            services.AddMidR(0, typeof(MediatorTests).Assembly);
            return services.BuildServiceProvider();
        }

        [Fact]
        public async Task Mediator_ImplementsBothSenderAndPublisher()
        {
            FakeNotificationHandlerA.Calls = new();

            using var scope = BuildProvider().CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var result = await mediator.SendAsync(new FakeRequest { Value = "med" });
            Assert.Equal("Handled:med", result);

            await mediator.PublishAsync(new FakeNotification { Message = "evt" });
            Assert.Contains("A:evt", FakeNotificationHandlerA.Calls);
        }

        [Fact]
        public void Mediator_IsResolvedAsScoped()
        {
            var provider = BuildProvider();
            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var m1 = scope1.ServiceProvider.GetRequiredService<IMediator>();
            var m2 = scope2.ServiceProvider.GetRequiredService<IMediator>();

            Assert.NotSame(m1, m2);
        }
    }
}

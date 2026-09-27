using Microsoft.Extensions.DependencyInjection;
using MidR.DependencyInjection;
using MidR.Interfaces;
using MidR.UnitTests.Fakes;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MidR.UnitTests
{
    public class SenderTests
    {
        private IServiceProvider BuildProvider(Action<MidRConfiguration>? configure = null)
        {
            var services = new ServiceCollection();
            var config = services.AddMidR(typeof(SenderTests).Assembly);
            configure?.Invoke(config);
            return services.BuildServiceProvider();
        }

        [Fact]
        public async Task SendAsync_WithRegisteredHandler_ReturnsExpectedResponse()
        {
            using var scope = BuildProvider().CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            var result = await sender.SendAsync(new FakeRequest { Value = "test" });

            Assert.Equal("Handled:test", result);
        }

        [Fact]
        public async Task SendAsync_VoidRequest_ExecutesHandler()
        {
            FakeVoidRequest.WasHandled = false;
            using var scope = BuildProvider().CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            await sender.SendAsync(new FakeVoidRequest());

            Assert.True(FakeVoidRequest.WasHandled);
        }

        [Fact]
        public async Task SendAsync_UnregisteredHandler_ThrowsInvalidOperationException()
        {
            using var scope = BuildProvider().CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => sender.SendAsync(new UnregisteredRequest()));
        }
    }
}

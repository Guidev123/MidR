using Microsoft.Extensions.DependencyInjection;
using MidR.DependencyInjection;
using MidR.Interfaces;
using MidR.UnitTests.Fakes;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MidR.UnitTests
{
    public class RequestBehaviorTests
    {
        private IServiceProvider BuildProvider(Action<MidRConfiguration>? configure = null)
        {
            var services = new ServiceCollection();
            var config = services.AddMidR(0, typeof(RequestBehaviorTests).Assembly);
            configure?.Invoke(config);
            return services.BuildServiceProvider();
        }

        [Fact]
        public async Task SendAsync_WithBehavior_ExecutesBehaviorAroundHandler()
        {
            FakeRequestBehavior<FakeRequest, string>.Calls = new();

            var provider = BuildProvider(config =>
                config.WithBehaviors(b =>
                    b.AddBehavior(typeof(FakeRequestBehavior<,>)).WithPriority(0)));

            using var scope = provider.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            var result = await sender.SendAsync(new FakeRequest { Value = "x" });

            Assert.Equal("Handled:x", result);
            Assert.Contains("Before", FakeRequestBehavior<FakeRequest, string>.Calls);
            Assert.Contains("After", FakeRequestBehavior<FakeRequest, string>.Calls);
            Assert.Equal(2, FakeRequestBehavior<FakeRequest, string>.Calls.Count);
        }

        [Fact]
        public async Task SendAsync_MultipleBehaviors_ExecutesByPriority()
        {
            FakeRequestBehavior<FakeRequest, string>.Calls = new();
            SecondRequestBehavior<FakeRequest, string>.Calls = new();

            var provider = BuildProvider(config =>
                config.WithBehaviors(b =>
                {
                    b.AddBehavior(typeof(FakeRequestBehavior<,>)).WithPriority(0);
                    b.AddBehavior(typeof(SecondRequestBehavior<,>)).WithPriority(1);
                }));

            using var scope = provider.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            await sender.SendAsync(new FakeRequest { Value = "y" });

            Assert.Contains("Before", FakeRequestBehavior<FakeRequest, string>.Calls);
            Assert.Contains("After", FakeRequestBehavior<FakeRequest, string>.Calls);
            Assert.Contains("SecondBefore", SecondRequestBehavior<FakeRequest, string>.Calls);
            Assert.Contains("SecondAfter", SecondRequestBehavior<FakeRequest, string>.Calls);
        }
    }
}

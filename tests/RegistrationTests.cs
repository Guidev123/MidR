using Microsoft.Extensions.DependencyInjection;
using MidR.DependencyInjection;
using MidR.Interfaces;
using MidR.UnitTests.Fakes;
using System;
using System.Reflection;
using Xunit;

namespace MidR.UnitTests
{
    public class RegistrationTests
    {
        [Fact]
        public void AddMidR_WithAssembly_RegistersAllServices()
        {
            var services = new ServiceCollection();
            services.AddMidR(0, typeof(RegistrationTests).Assembly);
            var provider = services.BuildServiceProvider();

            using var scope = provider.CreateScope();

            Assert.NotNull(scope.ServiceProvider.GetService<ISender>());
            Assert.NotNull(scope.ServiceProvider.GetService<IPublisher>());
            Assert.NotNull(scope.ServiceProvider.GetService<IMediator>());
        }

        [Fact]
        public void AddMidR_WithInvalidArgs_ThrowsArgumentException()
        {
            var services = new ServiceCollection();

            Assert.Throws<ArgumentException>(() =>
                services.AddMidR(0, 42));
        }

        [Fact]
        public void AddMidR_WithStringPrefix_ResolvesAssemblies()
        {
            var services = new ServiceCollection();
            services.AddMidR(0, "MidR");
            var provider = services.BuildServiceProvider();

            using var scope = provider.CreateScope();
            Assert.NotNull(scope.ServiceProvider.GetService<ISender>());
        }

        [Fact]
        public void AddMidR_RegistersNotificationHandlersAsMultiple()
        {
            var services = new ServiceCollection();
            services.AddMidR(0, typeof(RegistrationTests).Assembly);
            var provider = services.BuildServiceProvider();

            using var scope = provider.CreateScope();
            var handlers = scope.ServiceProvider.GetServices<INotificationHandler<FakeNotification>>();

            int count = 0;
            foreach (var _ in handlers) count++;
            Assert.Equal(2, count);
        }
    }
}

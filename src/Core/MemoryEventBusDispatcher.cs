using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MidR.Behaviors;
using MidR.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MidR.Core
{
    internal sealed class MemoryEventBusDispatcher : BackgroundService
    {
        private readonly MemoryBus _bus;
        private readonly IServiceProvider _serviceProvider;
        private readonly SemaphoreSlim _semaphore;
        private readonly ILogger<MemoryEventBusDispatcher> _logger;

        public MemoryEventBusDispatcher(MemoryBus bus, IServiceProvider serviceProvider, ILogger<MemoryEventBusDispatcher> logger)
        {
            _bus = bus;
            _serviceProvider = serviceProvider;
            _semaphore = new SemaphoreSlim(bus.MaxConcurrency);
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (INotification notification in _bus.Reader.ReadAllAsync(stoppingToken))
            {
                await _semaphore.WaitAsync(stoppingToken);

                _ = ExecuteNotificationAsync(notification, stoppingToken);
            }
        }

        private async Task ExecuteNotificationAsync(INotification notification, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();

                var pipeline = scope.ServiceProvider.GetRequiredService<INotificationBehaviorPipeline>();

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        "Processing notification {NotificationType}",
                        notification.GetType().Name);
                }

                await pipeline.ExecuteConcurrentAsync(
                    notification,
                    cancellationToken);

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        "Successfully processed notification {NotificationType}.",
                        notification.GetType().Name);
                }
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(
                        ex,
                        "Error while processing notification {NotificationType}. Payload: {@Notification}",
                        notification.GetType().Name,
                        notification);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override void Dispose()
        {
            _semaphore.Dispose();
            base.Dispose();
        }
    }
}
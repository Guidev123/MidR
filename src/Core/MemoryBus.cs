using MidR.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MidR.Core
{
    internal sealed class MemoryBus
    {
        private readonly Channel<INotification> _channel = Channel.CreateUnbounded<INotification>();

        internal int MaxConcurrency { get; }

        public MemoryBus(int maxConcurrency = 0)
        {
            MaxConcurrency = maxConcurrency > 0 ? maxConcurrency : Environment.ProcessorCount;
        }

        public ChannelReader<INotification> Reader => _channel.Reader;

        public async Task EnqueueAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            await _channel.Writer.WriteAsync(notification, cancellationToken);
        }
    }
}
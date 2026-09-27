using MidR.Interfaces;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MidR.Core
{
    internal sealed class MemoryBus
    {
        private readonly Channel<INotification> _channel;

        internal MemoryBusOptions Options { get; }

        public MemoryBus(MemoryBusOptions options)
        {
            Options = options;

            _channel = options.ChannelType switch
            {
                ChannelType.Unbounded => Channel.CreateUnbounded<INotification>(),
                ChannelType.Bounded when options.BoundedChannel is not null =>
                    Channel.CreateBounded<INotification>(options.BoundedChannel),
                _ => throw new ArgumentException(
                    $"Invalid {nameof(MemoryBusOptions)}: ChannelType is '{options.ChannelType}' but no valid channel could be built.",
                    nameof(options))
            };
        }

        public ChannelReader<INotification> Reader => _channel.Reader;

        public async Task EnqueueAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            await _channel.Writer.WriteAsync(notification, cancellationToken);
        }
    }

    public enum ChannelType
    {
        Bounded,
        Unbounded
    }

    public sealed class MemoryBusOptions
    {
        private MemoryBusOptions(int maxConcurrency, ChannelType channelType, BoundedChannelOptions? boundedChannelOptions = null)
        {
            MaxConcurrency = maxConcurrency > 0 ? maxConcurrency : Environment.ProcessorCount;
            ChannelType = channelType;
            BoundedChannel = boundedChannelOptions;
        }

        /// <summary>
        /// Creates options for a bounded in-memory bus channel — applies backpressure to
        /// <c>PublishToBusAsync</c> once <paramref name="boundedChannelOptions"/>'s capacity is reached,
        /// instead of letting the queue grow without limit.
        /// </summary>
        public static MemoryBusOptions CreateBounded(BoundedChannelOptions boundedChannelOptions, int maxConcurrency = 0)
        {
            return new MemoryBusOptions(
                maxConcurrency,
                ChannelType.Bounded,
                boundedChannelOptions
                );
        }

        /// <summary>
        /// Creates options for an unbounded in-memory bus channel — this is the default used by
        /// <c>AddMidR</c> when no explicit <see cref="MemoryBusOptions"/> is configured.
        /// </summary>
        public static MemoryBusOptions CreateUnbounded(int maxConcurrency = 0)
        {
            return new MemoryBusOptions(
                maxConcurrency,
                ChannelType.Unbounded
                );
        }

        internal int MaxConcurrency { get; }
        internal ChannelType ChannelType { get; }
        internal BoundedChannelOptions? BoundedChannel { get; }
    }
}

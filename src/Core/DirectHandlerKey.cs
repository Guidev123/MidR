using MidR.Abstractions;
using System;

namespace MidR.Core
{
    /// <summary>
    /// Composite key identifying a Direct Dispatch handler by notification type and routing key.
    /// </summary>
    internal readonly struct DirectHandlerKey : IEquatable<DirectHandlerKey>
    {
        public DirectHandlerKey(Type notificationType, RoutingKey key)
        {
            NotificationType = notificationType;
            Key = key;
        }

        public Type NotificationType { get; }

        public RoutingKey Key { get; }

        public bool Equals(DirectHandlerKey other) =>
            NotificationType == other.NotificationType && Key.Equals(other.Key);

        public override bool Equals(object? obj) => obj is DirectHandlerKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (NotificationType.GetHashCode() * 397) ^ Key.GetHashCode();
            }
        }

        public override string ToString() => $"({NotificationType.Name}, '{Key}')";
    }
}

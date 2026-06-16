using System;

namespace MidR.Abstractions
{
    /// <summary>
    /// Identifies a single Direct Dispatch destination for a notification.
    /// A handler is bound to a routing key via <see cref="DirectQueueAttribute"/>, and a
    /// notification is routed to that handler by publishing with the matching key.
    /// </summary>
    /// <remarks>
    /// The comparison is case-sensitive and ordinal. As a value type with structural equality,
    /// it can be used as a dictionary key on the publish hot path without allocations or boxing.
    /// </remarks>
    public readonly struct RoutingKey : IEquatable<RoutingKey>
    {
        private readonly string _value;

        /// <summary>
        /// Creates a new <see cref="RoutingKey"/> from the specified value.
        /// </summary>
        /// <param name="value">The routing key value. Must not be null or whitespace.</param>
        public RoutingKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A routing key must not be null or whitespace.", nameof(value));
            }

            _value = value;
        }

        /// <summary>
        /// Gets the underlying routing key value.
        /// </summary>
        public string Value => _value ?? string.Empty;

        /// <summary>
        /// Implicitly converts a <see cref="string"/> into a <see cref="RoutingKey"/>.
        /// </summary>
        public static implicit operator RoutingKey(string value) => new RoutingKey(value);

        /// <inheritdoc />
        public bool Equals(RoutingKey other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is RoutingKey other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

        /// <inheritdoc />
        public override string ToString() => Value;

        public static bool operator ==(RoutingKey left, RoutingKey right) => left.Equals(right);

        public static bool operator !=(RoutingKey left, RoutingKey right) => !left.Equals(right);
    }
}

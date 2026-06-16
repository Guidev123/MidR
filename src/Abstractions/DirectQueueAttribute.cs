using System;

namespace MidR.Abstractions
{
    /// <summary>
    /// Marks a notification handler as a <em>Direct Dispatch</em> consumer bound to a specific
    /// <see cref="RoutingKey"/>.
    /// </summary>
    /// <remarks>
    /// A handler decorated with this attribute is <strong>excluded from the normal fan-out</strong>
    /// (<c>PublishAsync</c> without a routing key) and is only invoked when a notification is
    /// published with the matching key. This preserves the guaranteed-delivery semantics required
    /// by patterns such as the Inbox Pattern, where an entry must reach exactly one owning consumer.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class DirectQueueAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DirectQueueAttribute"/> class.
        /// </summary>
        /// <param name="key">The routing key the handler is bound to. Must not be null or whitespace.</param>
        public DirectQueueAttribute(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("A direct queue routing key must not be null or whitespace.", nameof(key));
            }

            Key = key;
        }

        /// <summary>
        /// Gets the routing key the handler is bound to.
        /// </summary>
        public string Key { get; }
    }
}

using System;
using System.Collections.Generic;

namespace MidR.Behaviors
{
    /// <summary>
    /// Represents the configuration container for request pipeline behaviors.
    /// </summary>
    public sealed class BehaviorConfiguration
    {
        internal readonly List<BehaviorDescriptor> Behaviors = new();

        /// <summary>
        /// Registers a new behavior type in the pipeline configuration.
        /// </summary>
        /// <param name="behaviorType">
        /// The type of the behavior to be added. Must implement <see cref="IRequestBehavior{TRequest, TResponse}"/>.
        /// </param>
        /// <returns>
        /// A <see cref="BehaviorRegistration"/> instance used to further configure the behavior.
        /// </returns>
        public BehaviorRegistration AddBehavior(Type behaviorType)
        {
            return new BehaviorRegistration(this, behaviorType);
        }
    }
}
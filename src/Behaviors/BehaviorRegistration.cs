using System;

namespace MidR.Behaviors
{
    /// <summary>
    /// Provides a fluent API to configure a behavior registration.
    /// </summary>
    public sealed class BehaviorRegistration
    {
        private readonly BehaviorConfiguration _configuration;
        private readonly Type _behaviorType;

        internal BehaviorRegistration(BehaviorConfiguration configuration, Type behaviorType)
        {
            _configuration = configuration;
            _behaviorType = behaviorType;
        }

        /// <summary>
        /// Sets the execution priority of the behavior and finalizes its registration.
        /// </summary>
        /// <param name="priority">
        /// The priority value. Lower values indicate earlier execution in the pipeline.
        /// </param>
        public void WithPriority(int priority)
        {
            _configuration.Behaviors.Add(new BehaviorDescriptor(_behaviorType, priority));
        }
    }
}
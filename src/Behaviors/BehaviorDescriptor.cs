using System;

namespace MidR.Behaviors
{
    /// <summary>
    /// Describes a registered pipeline behavior, including its type and execution priority.
    /// </summary>
    internal sealed class BehaviorDescriptor
    {
        /// <summary>
        /// Gets the type of the behavior.
        /// </summary>
        public Type BehaviorType { get; }

        /// <summary>
        /// Gets the execution priority of the behavior.
        /// Lower values typically execute earlier in the pipeline.
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BehaviorDescriptor"/> class.
        /// </summary>
        /// <param name="behaviorType">The type of the behavior.</param>
        /// <param name="priority">The execution priority.</param>
        public BehaviorDescriptor(Type behaviorType, int priority)
        {
            BehaviorType = behaviorType;
            Priority = priority;
        }
    }
}
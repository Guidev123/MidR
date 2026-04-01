using MidR.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MidR.Behaviors
{
    internal abstract class BehaviorExecutor
    {
        public int Priority { get; }
        public Type ConcreteBehaviorType { get; }

        protected BehaviorExecutor(Type concreteBehaviorType, int priority)
        {
            ConcreteBehaviorType = concreteBehaviorType;
            Priority = priority;
        }

        public abstract Task<TResponse> ExecuteTypedAsync<TResponse>(object request, Func<Task<TResponse>> next, IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }

    internal sealed class BehaviorExecutor<TRequest, TResponse> : BehaviorExecutor
        where TRequest : class, IRequest<TResponse>
    {
        private readonly Type _closedConcreteBehaviorType;

        public BehaviorExecutor(Type concreteBehaviorType, int priority) : base(concreteBehaviorType, priority)
        {
            _closedConcreteBehaviorType = concreteBehaviorType.IsGenericTypeDefinition
                ? concreteBehaviorType.MakeGenericType(typeof(TRequest), typeof(TResponse))
                : concreteBehaviorType;
        }

        public override async Task<TResp> ExecuteTypedAsync<TResp>(object request, Func<Task<TResp>> next, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            if (serviceProvider.GetService(_closedConcreteBehaviorType) is not IRequestBehavior<TRequest, TResponse> behavior)
            {
                return await next();
            }

            var result = await behavior.ExecuteAsync((TRequest)request, async () =>
            {
                var nextResult = await next();

                if (nextResult is TResponse typed)
                {
                    return typed;
                }

                return default!;
            }, cancellationToken);

            return (TResp)(object)result!;
        }
    }
}
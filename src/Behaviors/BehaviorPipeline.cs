using MidR.Core;
using MidR.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MidR.Behaviors
{
    internal sealed class BehaviorPipeline : IBehaviorPipeline
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly HandlerRegistry _handlerRegistry;

        public BehaviorPipeline(IServiceProvider serviceProvider, HandlerRegistry handlerRegistry)
        {
            _serviceProvider = serviceProvider;
            _handlerRegistry = handlerRegistry;
        }

        public Task<TResponse> ExecuteAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            var requestType = request.GetType();

            if (_handlerRegistry.HasHandler(requestType) is false)
            {
                throw new InvalidOperationException($"Handler not found for {requestType.Name}");
            }

            var behaviors = _handlerRegistry.GetBehaviorsArray(requestType);

            if (behaviors.Length == 0)
            {
                return _handlerRegistry.ExecuteHandlerAsync(request, _serviceProvider, cancellationToken);
            }

            return ExecutePipelineAsync(request, behaviors, 0, _serviceProvider, cancellationToken);
        }

        private Task<TResponse> ExecutePipelineAsync<TResponse>(
            IRequest<TResponse> request,
            BehaviorExecutor[] behaviors,
            int index,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken)
        {
            if (index >= behaviors.Length)
            {
                return _handlerRegistry.ExecuteHandlerAsync(request, serviceProvider, cancellationToken);
            }

            var behavior = behaviors[index];
            return behavior.ExecuteTypedAsync(
                request,
                () => ExecutePipelineAsync(request, behaviors, index + 1, serviceProvider, cancellationToken),
                serviceProvider,
                cancellationToken);
        }
    }
}
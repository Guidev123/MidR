using MidR.Behaviors;
using MidR.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace MidR.Core
{
    internal sealed class Sender : ISender
    {
        private readonly IBehaviorPipeline _pipeline;

        public Sender(IBehaviorPipeline pipeline)
        {
            _pipeline = pipeline;
        }

        public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            return _pipeline.ExecuteAsync(request, cancellationToken);
        }
    }
}
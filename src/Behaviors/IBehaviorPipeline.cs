using MidR.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace MidR.Behaviors
{
    internal interface IBehaviorPipeline
    {
        Task<TResponse> ExecuteAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    }
}
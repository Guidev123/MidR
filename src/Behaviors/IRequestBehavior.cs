using MidR.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace MidR.Behaviors
{
    /// <summary>
    /// Represents a pipeline behavior that can intercept and wrap request handling logic.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    public interface IRequestBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        /// <summary>
        /// Executes the behavior logic around the next delegate in the pipeline.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <param name="next">
        /// The next delegate in the pipeline. Invoking this continues the execution chain.
        /// </param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The response produced by the pipeline.</returns>
        Task<TResponse> ExecuteAsync(
            TRequest request,
            RequestDelegate<TResponse> next,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Represents the next delegate in the request pipeline.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <returns>A task that produces the response.</returns>
    public delegate Task<TResponse> RequestDelegate<TResponse>();
}
using MidR.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace MidR.Interfaces
{
    /// <summary>
    /// Defines a handler that processes a specific type of request and returns a response.
    /// </summary>
    /// <typeparam name="TRequest">
    /// The type of the request to handle. Must implement <see cref="IRequest{TResponse}"/>.
    /// </typeparam>
    /// <typeparam name="TResponse">The type of the response returned by the handler.</typeparam>
    public interface IRequestHandler<in TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        /// <summary>
        /// Executes the handling logic for the specified request asynchronously and returns a response.
        /// </summary>
        /// <param name="request">The request instance to handle.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation, containing the response.</returns>
        Task<TResponse> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Defines a handler that processes a specific type of request without response.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    public interface IRequestHandler<in TRequest> : IRequestHandler<TRequest, Unit>
        where TRequest : IRequest
    { }
}
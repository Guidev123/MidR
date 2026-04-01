using System.Threading;
using System.Threading.Tasks;

namespace MidR.Interfaces
{
    /// <summary>
    /// Provides functionality to send requests and receive responses
    /// following the request/response pattern.
    /// </summary>
    public interface ISender
    {
        /// <summary>
        /// Sends a request to its corresponding handler and returns a response.
        /// </summary>
        /// <typeparam name="TResponse">The type of response expected.</typeparam>
        /// <param name="request">The request instance.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The response returned by the handler.</returns>
        Task<TResponse> SendAsync<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default);
    }
}
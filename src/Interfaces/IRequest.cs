using MidR.Abstractions;

namespace MidR.Interfaces
{
    /// <summary>
    /// Represents a request that expects a response of the specified type.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response returned by the request.</typeparam>
    public interface IRequest<out TResponse>
    { }

    /// <summary>
    /// Represents a request that expects no response.
    /// </summary>
    public interface IRequest : IRequest<Unit>
    { }
}
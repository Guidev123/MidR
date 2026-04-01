namespace MidR.Interfaces
{
    /// <summary>
    /// Defines a mediator abstraction that combines request/response (sending)
    /// and publish/subscribe (notification) capabilities.
    /// </summary>
    public interface IMediator : ISender, IPublisher
    {
    }
}
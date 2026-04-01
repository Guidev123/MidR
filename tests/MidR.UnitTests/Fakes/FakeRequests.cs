using MidR.Abstractions;
using MidR.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace MidR.UnitTests.Fakes
{
    public class FakeRequest : IRequest<string>
    {
        public string Value { get; set; } = string.Empty;
    }

    public class FakeRequestHandler : IRequestHandler<FakeRequest, string>
    {
        public Task<string> ExecuteAsync(FakeRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult($"Handled:{request.Value}");
        }
    }

    public class FakeVoidRequest : IRequest
    {
        public static bool WasHandled { get; set; }
    }

    public class FakeVoidRequestHandler : IRequestHandler<FakeVoidRequest>
    {
        public Task<Unit> ExecuteAsync(FakeVoidRequest request, CancellationToken cancellationToken = default)
        {
            FakeVoidRequest.WasHandled = true;
            return Task.FromResult(Unit.Value);
        }
    }

    public class UnregisteredRequest : IRequest<string>
    {
    }
}

// ABOUTME: Application boundary for verifying Listmonk generated-client connectivity.
// ABOUTME: Implemented by Infrastructure so handlers never reference the generated NSwag client.

namespace Explore.Application.Contracts.Infrastructure;

public interface IListmonkConnectionTester
{
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}

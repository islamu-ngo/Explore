// ABOUTME: Coordinates one ATProto OAuth refresh for an exact tenant/user/provider/DID session.
// ABOUTME: Exposes only an async lease so Application and Infrastructure stay independent of PostgreSQL.

namespace Explore.Application.Contracts.Persistence;

public interface IAtprotoSessionRefreshLock
{
    Task<IAsyncDisposable> AcquireAsync(
        Guid tenantId,
        Guid userId,
        string provider,
        string subjectDid,
        CancellationToken cancellationToken = default);
}

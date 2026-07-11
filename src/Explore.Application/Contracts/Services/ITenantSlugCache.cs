// ABOUTME: Exposes normalized tenant slug and domain lookups for resolver components.
// ABOUTME: Keeps resolver consumers independent from cache implementation details and refresh mechanics.

namespace Explore.Application.Contracts.Services;

public interface ITenantSlugCache
{
    Task WarmAsync(CancellationToken cancellationToken = default);

    Task RefreshAsync(CancellationToken cancellationToken = default);

    ValueTask<Guid?> GetTenantIdBySlugAsync(string slug, CancellationToken cancellationToken = default);

    ValueTask<Guid?> GetTenantIdByDomainAsync(string domain, CancellationToken cancellationToken = default);
}

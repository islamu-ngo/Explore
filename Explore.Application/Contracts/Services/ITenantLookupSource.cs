// ABOUTME: Provides tenant lookup data to runtime caches without exposing persistence details.
// ABOUTME: Lets Infrastructure load slug and domain mappings through an Application-layer contract.

using Explore.Application.Models.Tenants;

namespace Explore.Application.Contracts.Services;

public interface ITenantLookupSource
{
    Task<IReadOnlyList<TenantLookupRecord>> GetTenantLookupsAsync(CancellationToken cancellationToken = default);
}

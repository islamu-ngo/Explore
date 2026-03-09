// ABOUTME: Stores the resolved tenant for the current request or Blazor circuit scope.
// ABOUTME: Lets tenant resolution and tenant consumption evolve independently during the Phase 2.4 split.

namespace Explore.Application.Contracts.Services;

public interface ITenantContextAccessor
{
    Guid? TenantId { get; }

    bool IsResolved { get; }

    void SetTenant(Guid tenantId);

    void Clear();
}

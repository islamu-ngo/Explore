// ABOUTME: Persistence contract for active and historical paid-event policy versions.
// ABOUTME: Returns Domain entities so policy revision flows keep entity-owned invariants.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IPaidEventPolicyRepository
{
    const int MaximumActiveTenantPolicyPageSize = 256;

    Task<PaidEventPolicyVersion?> GetActiveInstanceAsync(CancellationToken cancellationToken);

    Task<PaidEventPolicyVersion?> GetActiveTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<PaidEventPolicyVersion[]> ListActiveTenantsAsync(
        int offset,
        int limit,
        CancellationToken cancellationToken);

    Task<PaidEventPolicyVersion[]> ListTenantHistoryAsync(Guid tenantId, CancellationToken cancellationToken);

    Task AddAsync(PaidEventPolicyVersion policy, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

// ABOUTME: Defines persistence access for tenant-local group participation and hierarchy policy.
// ABOUTME: Keeps global group identity separate from tenant approval, membership, and profile state.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IGroupTenantRepository : IGenericRepository<GroupTenant, Guid>
{
    Task<GroupTenant?> GetByGroupAndTenant(
        Guid groupId,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

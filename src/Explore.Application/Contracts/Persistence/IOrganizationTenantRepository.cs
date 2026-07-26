// ABOUTME: Defines persistence access for tenant-local organization participation and policy.
// ABOUTME: Keeps global organization identity separate from tenant approval, membership, and profile state.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IOrganizationTenantRepository : IGenericRepository<OrganizationTenant, Guid>
{
    Task<OrganizationTenant?> GetByOrganizationAndTenant(
        Guid organizationId,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

// ABOUTME: Repository contract for tenant administrator assignments and role-scoped membership checks.
// ABOUTME: Provides tenant/user lookup helpers for tenant-level authorization workflows.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface ITenantAdministratorRepository : IGenericRepository<TenantAdministrator, Guid>
{
    Task<TenantAdministrator?> GetByTenantAndUser(Guid tenantId, Guid userId);
    Task<List<TenantAdministrator>> GetByTenant(Guid tenantId);
    Task<bool> IsTenantAdministrator(Guid tenantId, Guid userId);
}

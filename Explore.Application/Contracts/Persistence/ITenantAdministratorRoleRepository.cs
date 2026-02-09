// ABOUTME: Repository contract for tenant administrator role lookup records.
// ABOUTME: Resolves role metadata by master code for deterministic assignment behavior.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface ITenantAdministratorRoleRepository : IGenericRepository<TenantAdministratorRole, int>
{
    Task<TenantAdministratorRole?> GetByMasterCode(string masterCode);
}

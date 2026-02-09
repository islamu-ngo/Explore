using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface ITenantSettingsRepository : IGenericRepository<TenantSettings, Guid>
{
    Task<TenantSettings?> GetByTenant(Guid tenantId);
    Task<TenantSettings?> GetTenantSettingsWithDetails(Guid id);
    Task<List<TenantSettings>> GetTenantSettingsListWithDetails();
}

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

[Obsolete("Use ISettingsResolver for reads and IInstanceGovernanceSettingService/ITenantPolicySettingService for writes. Will be removed with TenantSettings entity in Phase 4.5.")]
public interface ITenantSettingsRepository : IGenericRepository<TenantSettings, Guid>
{
    Task<TenantSettings?> GetByTenant(Guid tenantId);
    Task<TenantSettings?> GetTenantSettingsWithDetails(Guid id);
    Task<List<TenantSettings>> GetTenantSettingsListWithDetails();
}

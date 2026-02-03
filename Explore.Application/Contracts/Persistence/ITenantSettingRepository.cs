// ABOUTME: Repository interface for TenantSetting entity providing data access
// for tenant-specific setting overrides.

namespace Explore.Application.Contracts.Persistence;

using Explore.Domain;

/// <summary>
/// Repository for tenant-specific setting overrides.
/// </summary>
public interface ITenantSettingRepository : IGenericRepository<TenantSetting, Guid>
{
    /// <summary>
    /// Gets a tenant's override for a specific setting key.
    /// </summary>
    Task<TenantSetting?> GetByTenantAndKey(Guid tenantId, string key);

    /// <summary>
    /// Gets all overrides for a tenant.
    /// </summary>
    Task<List<TenantSetting>> GetAllForTenant(Guid tenantId);

    /// <summary>
    /// Removes a tenant's override for a specific setting key.
    /// </summary>
    Task<bool> RemoveOverride(Guid tenantId, string key);
}

// ABOUTME: Repository implementation for TenantSetting entity providing data access
// for tenant-specific setting overrides.

namespace Explore.Persistence.Repositories;

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

public class TenantSettingRepository : GenericRepository<TenantSetting, Guid>, ITenantSettingRepository
{
    private readonly ExploreDbContext _dbContext;

    public TenantSettingRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TenantSetting?> GetByTenantAndKey(Guid tenantId, string key)
    {
        return await _dbContext.TenantSettingOverrides
            .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.SettingKey == key);
    }

    public async Task<List<TenantSetting>> GetAllForTenant(Guid tenantId)
    {
        return await _dbContext.TenantSettingOverrides
            .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .ToListAsync();
    }

    public async Task<bool> RemoveOverride(Guid tenantId, string key)
    {
        var setting = await _dbContext.TenantSettingOverrides
            .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.SettingKey == key);

        if (setting == null)
            return false;

        _dbContext.TenantSettingOverrides.Remove(setting);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> LockAsync(Guid tenantId, string key)
    {
        var setting = await _dbContext.TenantSettingOverrides
            .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.SettingKey == key);

        if (setting == null)
            return false;

        setting.IsLocked = true;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnlockAsync(Guid tenantId, string key)
    {
        var setting = await _dbContext.TenantSettingOverrides
            .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.SettingKey == key);

        if (setting == null)
            return false;

        setting.IsLocked = false;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<List<TenantSetting>> GetLockedForTenant(Guid tenantId)
    {
        return await _dbContext.TenantSettingOverrides
            .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.IsLocked)
            .ToListAsync();
    }
}

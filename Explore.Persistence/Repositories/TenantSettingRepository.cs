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

    public async Task UpsertManyForTenantAsync(
        Guid tenantId,
        IReadOnlyCollection<TenantSettingOverrideUpsert> overrides,
        CancellationToken cancellationToken = default)
    {
        if (overrides.Count == 0)
        {
            return;
        }

        string[] keys = overrides.Select(overrideValue => overrideValue.SettingKey).Distinct().ToArray();
        List<TenantSetting> existing = await _dbContext.TenantSettingOverrides
            .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
            .Where(setting => setting.TenantId == tenantId && keys.Contains(setting.SettingKey))
            .ToListAsync(cancellationToken);

        Dictionary<string, TenantSetting> existingByKey = existing.ToDictionary(setting => setting.SettingKey);
        foreach (TenantSettingOverrideUpsert overrideValue in overrides)
        {
            if (existingByKey.TryGetValue(overrideValue.SettingKey, out TenantSetting? setting))
            {
                setting.Value = overrideValue.Value;
                setting.IsLocked = overrideValue.IsLocked;
                continue;
            }

            _dbContext.TenantSettingOverrides.Add(new TenantSetting
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Tenant = null!,
                SettingKey = overrideValue.SettingKey,
                Value = overrideValue.Value,
                IsLocked = overrideValue.IsLocked
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

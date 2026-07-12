// ABOUTME: Repository implementation for TenantSetting entity providing data access.
// ABOUTME: Resolves tenant overrides and normalized cross-tenant domain-host ownership.

namespace Explore.Persistence.Repositories;

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

public class TenantSettingRepository : ITenantSettingRepository
{
    private readonly ExploreDbContext _dbContext;

    public TenantSettingRepository(ExploreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TenantSetting?> GetByTenantAndKey(
        Guid tenantId,
        string key,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TenantSettingOverrides
            .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.SettingKey == key, cancellationToken);
    }

    public Task<TenantSetting?> GetByDomainHostAsync(
        string normalizedHost,
        CancellationToken cancellationToken = default)
    {
        string normalizedValue = normalizedHost.Trim().TrimEnd('.').ToLowerInvariant();
        return _dbContext.TenantSettingOverrides
            .FromSqlInterpolated(
                $$"""
                SELECT *
                FROM tenant_setting_overrides
                WHERE setting_key IN ('domains.tenant_subdomain', 'domains.tenant_custom_domain')
                  AND rtrim(lower(btrim(value::jsonb #>> '{}')), '.') = {{normalizedValue}}
                """)
            .IgnoreTenantFilter(TenantFilterBypassReasons.ManagedTenantDomainUniqueness)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SetValueAsync(
        Guid tenantId,
        string key,
        string value,
        CancellationToken cancellationToken = default,
        Guid? actorId = null)
    {
        DateTime now = DateTime.UtcNow;
        int updated = await _dbContext.TenantSettingOverrides
            .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
            .Where(setting => setting.TenantId == tenantId && setting.SettingKey == key)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(setting => setting.Value, value)
                    .SetProperty(setting => setting.UpdatedAt, now)
                    .SetProperty(setting => setting.UpdatedBy, actorId),
                cancellationToken);

        if (updated > 0)
        {
            return;
        }

        _dbContext.TenantSettingOverrides.Add(new TenantSetting
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Tenant = null!,
            SettingKey = key,
            Value = value,
            IsLocked = false,
            CreatedAt = now,
            CreatedBy = actorId
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<TenantSetting>> GetAllForTenant(Guid tenantId)
    {
        return await _dbContext.TenantSettingOverrides
            .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .ToListAsync();
    }

    public async Task<bool> RemoveOverrideAsync(
        Guid tenantId,
        string key,
        CancellationToken cancellationToken = default)
    {
        int removed = await _dbContext.TenantSettingOverrides
            .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
            .Where(setting => setting.TenantId == tenantId && setting.SettingKey == key)
            .ExecuteDeleteAsync(cancellationToken);

        return removed > 0;
    }

    public async Task<bool> LockAsync(
        Guid tenantId,
        string key,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;
        int updated = await _dbContext.TenantSettingOverrides
            .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
            .Where(setting => setting.TenantId == tenantId
                && setting.SettingKey == key
                && !setting.IsLocked)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(setting => setting.IsLocked, true)
                    .SetProperty(setting => setting.UpdatedAt, now)
                    .SetProperty(setting => setting.UpdatedBy, actorId),
                cancellationToken);

        return updated > 0;
    }

    public async Task<bool> UnlockAsync(
        Guid tenantId,
        string key,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;
        int updated = await _dbContext.TenantSettingOverrides
            .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
            .Where(setting => setting.TenantId == tenantId
                && setting.SettingKey == key
                && setting.IsLocked)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(setting => setting.IsLocked, false)
                    .SetProperty(setting => setting.UpdatedAt, now)
                    .SetProperty(setting => setting.UpdatedBy, actorId),
                cancellationToken);

        return updated > 0;
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
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        if (overrides.Count == 0)
        {
            return;
        }

        DateTime now = DateTime.UtcNow;
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
                setting.UpdatedAt = now;
                setting.UpdatedBy = actorId;
                continue;
            }

            _dbContext.TenantSettingOverrides.Add(new TenantSetting
            {
                Id = Guid.CreateVersion7(),
                TenantId = tenantId,
                Tenant = null!,
                SettingKey = overrideValue.SettingKey,
                Value = overrideValue.Value,
                IsLocked = overrideValue.IsLocked,
                CreatedAt = now,
                CreatedBy = actorId
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

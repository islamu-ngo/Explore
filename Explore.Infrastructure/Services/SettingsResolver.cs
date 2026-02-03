// ABOUTME: Implementation of the cascading settings resolver with caching support.
// Resolves settings through 3-tier cascade: System (locked check) -> Tenant override -> System default.
// Uses repositories for data access following Clean Architecture.

namespace Explore.Infrastructure.Services;

using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;

/// <summary>
/// Resolves settings through the cascading settings engine with caching.
/// </summary>
public class SettingsResolver : ISettingsResolver
{
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly ITenantSettingRepository _tenantSettingRepository;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    private const string SystemSettingsCacheKey = "SystemSettings_All";
    private const string TenantSettingsCacheKeyPrefix = "TenantSettings_";

    public SettingsResolver(
        ISystemSettingRepository systemSettingRepository,
        ITenantSettingRepository tenantSettingRepository,
        IMemoryCache cache)
    {
        _systemSettingRepository = systemSettingRepository;
        _tenantSettingRepository = tenantSettingRepository;
        _cache = cache;
    }

    public async Task<T?> GetSettingAsync<T>(string key, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var resolved = await GetSettingWithMetadataAsync(key, tenantId, cancellationToken);
        if (resolved == null)
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(resolved.Value);
        }
        catch
        {
            return default;
        }
    }

    public async Task<ResolvedSetting?> GetSettingWithMetadataAsync(string key, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        // Get system setting first
        var systemSettings = await GetSystemSettingsAsync(cancellationToken);
        var systemSetting = systemSettings.FirstOrDefault(s => s.Key == key);

        if (systemSetting == null)
            return null;

        // If locked or no tenant specified, return system value
        if (systemSetting.IsLocked || tenantId == null)
        {
            return new ResolvedSetting
            {
                Key = systemSetting.Key,
                Value = systemSetting.Value,
                ValueType = systemSetting.ValueType,
                Source = systemSetting.IsLocked ? SettingSource.SystemLocked : SettingSource.SystemDefault,
                IsLocked = systemSetting.IsLocked,
                Description = systemSetting.Description,
                Category = systemSetting.Category,
                AllowedValues = systemSetting.AllowedValues
            };
        }

        // Check for tenant override
        var tenantSettings = await GetTenantSettingsAsync(tenantId.Value, cancellationToken);
        var tenantOverride = tenantSettings.FirstOrDefault(s => s.Key == key);

        if (tenantOverride != null)
        {
            return new ResolvedSetting
            {
                Key = key,
                Value = tenantOverride.Value,
                ValueType = systemSetting.ValueType,
                Source = SettingSource.TenantOverride,
                IsLocked = false,
                Description = systemSetting.Description,
                Category = systemSetting.Category,
                AllowedValues = systemSetting.AllowedValues
            };
        }

        // Fall back to system default
        return new ResolvedSetting
        {
            Key = systemSetting.Key,
            Value = systemSetting.Value,
            ValueType = systemSetting.ValueType,
            Source = SettingSource.SystemDefault,
            IsLocked = false,
            Description = systemSetting.Description,
            Category = systemSetting.Category,
            AllowedValues = systemSetting.AllowedValues
        };
    }

    public async Task<IReadOnlyList<ResolvedSetting>> GetAllSettingsAsync(Guid? tenantId = null, string? category = null, CancellationToken cancellationToken = default)
    {
        var systemSettings = await GetSystemSettingsAsync(cancellationToken);
        var tenantSettings = tenantId.HasValue
            ? await GetTenantSettingsAsync(tenantId.Value, cancellationToken)
            : new List<TenantSetting>();

        var tenantSettingsDict = tenantSettings.ToDictionary(s => s.Key, s => s.Value);

        var result = systemSettings
            .Where(s => category == null || s.Category == category)
            .Select(s =>
            {
                var hasOverride = tenantSettingsDict.TryGetValue(s.Key, out var overrideValue);
                var effectiveValue = s.IsLocked || !hasOverride ? s.Value : overrideValue!;
                var source = s.IsLocked ? SettingSource.SystemLocked
                    : hasOverride ? SettingSource.TenantOverride
                    : SettingSource.SystemDefault;

                return new ResolvedSetting
                {
                    Key = s.Key,
                    Value = effectiveValue,
                    ValueType = s.ValueType,
                    Source = source,
                    IsLocked = s.IsLocked,
                    Description = s.Description,
                    Category = s.Category,
                    AllowedValues = s.AllowedValues
                };
            })
            .ToList();

        return result;
    }

    public async Task<bool> CanOverrideAsync(string key, CancellationToken cancellationToken = default)
    {
        return !await _systemSettingRepository.IsLocked(key);
    }

    public async Task<bool> SetTenantOverrideAsync(string key, object value, Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Check if setting exists and is not locked
        if (!await CanOverrideAsync(key, cancellationToken))
            return false;

        var existingOverride = await _tenantSettingRepository.GetByTenantAndKey(tenantId, key);
        var jsonValue = JsonSerializer.Serialize(value);

        if (existingOverride != null)
        {
            existingOverride.Value = jsonValue;
            existingOverride.UpdatedAt = DateTime.UtcNow;
            await _tenantSettingRepository.Update(existingOverride);
        }
        else
        {
            var newOverride = new TenantSetting
            {
                TenantId = tenantId,
                Key = key,
                Value = jsonValue,
                CreatedAt = DateTime.UtcNow
            };
            await _tenantSettingRepository.Create(newOverride);
        }

        InvalidateCache(key, tenantId);
        return true;
    }

    public async Task<bool> RemoveTenantOverrideAsync(string key, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var result = await _tenantSettingRepository.RemoveOverride(tenantId, key);
        if (result)
        {
            InvalidateCache(key, tenantId);
        }
        return result;
    }

    public void InvalidateCache(string? key = null, Guid? tenantId = null)
    {
        if (key == null && tenantId == null)
        {
            _cache.Remove(SystemSettingsCacheKey);
        }
        else if (tenantId.HasValue)
        {
            _cache.Remove($"{TenantSettingsCacheKeyPrefix}{tenantId}");
        }
        else
        {
            _cache.Remove(SystemSettingsCacheKey);
        }
    }

    private async Task<List<SystemSetting>> GetSystemSettingsAsync(CancellationToken cancellationToken)
    {
        return await _cache.GetOrCreateAsync(SystemSettingsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheExpiration;
            return await _systemSettingRepository.GetAllSettings();
        }) ?? new List<SystemSetting>();
    }

    private async Task<List<TenantSetting>> GetTenantSettingsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var cacheKey = $"{TenantSettingsCacheKeyPrefix}{tenantId}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheExpiration;
            return await _tenantSettingRepository.GetAllForTenant(tenantId);
        }) ?? new List<TenantSetting>();
    }
}

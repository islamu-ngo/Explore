// ABOUTME: Implementation of the cascading settings resolver with caching support.
// Resolves settings through 3-tier cascade: System (locked check) -> Tenant override -> System default.
// Uses repositories for data access following Clean Architecture.

namespace Explore.Infrastructure.Services;

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.Extensions.Caching.Memory;

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

    public async Task<T?> GetSettingAsync<T>(string settingKey, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var resolved = await GetSettingWithMetadataAsync(settingKey, tenantId, cancellationToken);
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

    public async Task<ResolvedSetting?> GetSettingWithMetadataAsync(string settingKey, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        // Get system setting first
        var systemSettings = await GetSystemSettingsAsync(cancellationToken);
        var systemSetting = systemSettings.FirstOrDefault(s => s.SettingKey == settingKey);

        if (systemSetting == null)
            return null;

        // If locked or no tenant specified, return system value
        if (systemSetting.IsLocked || tenantId == null)
        {
            return new ResolvedSetting
            {
                Key = systemSetting.SettingKey,
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
        var tenantOverride = tenantSettings.FirstOrDefault(s => s.SettingKey == settingKey);

        if (tenantOverride != null)
        {
            return new ResolvedSetting
            {
                Key = settingKey,
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
            Key = systemSetting.SettingKey,
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

        var tenantSettingsDict = tenantSettings.ToDictionary(s => s.SettingKey, s => s.Value);

        var result = systemSettings
            .Where(s => category == null || s.Category == category)
            .Select(s =>
            {
                var hasOverride = tenantSettingsDict.TryGetValue(s.SettingKey, out var overrideValue);
                var effectiveValue = s.IsLocked || !hasOverride ? s.Value : overrideValue!;
                var source = s.IsLocked ? SettingSource.SystemLocked
                    : hasOverride ? SettingSource.TenantOverride
                    : SettingSource.SystemDefault;

                return new ResolvedSetting
                {
                    Key = s.SettingKey,
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

    public async Task<bool> CanOverrideAsync(string settingKey, CancellationToken cancellationToken = default)
    {
        return !await _systemSettingRepository.IsLocked(settingKey);
    }

    public async Task<bool> SetTenantOverrideAsync(string settingKey, object value, Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Check if setting exists and is not locked
        if (!await CanOverrideAsync(settingKey, cancellationToken))
            return false;

        var existingOverride = await _tenantSettingRepository.GetByTenantAndKey(tenantId, settingKey);
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
                Tenant = null!,
                SettingKey = settingKey,
                Value = jsonValue,
                CreatedAt = DateTime.UtcNow
            };
            await _tenantSettingRepository.Create(newOverride);
        }

        InvalidateCache(settingKey, tenantId);
        return true;
    }

    public async Task<bool> RemoveTenantOverrideAsync(string settingKey, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var result = await _tenantSettingRepository.RemoveOverride(tenantId, settingKey);
        if (result)
        {
            InvalidateCache(settingKey, tenantId);
        }
        return result;
    }

    public void InvalidateCache(string? settingKey = null, Guid? tenantId = null)
    {
        if (settingKey == null && tenantId == null)
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

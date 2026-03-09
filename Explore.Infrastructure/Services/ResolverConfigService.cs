// ABOUTME: Reads and writes tenant resolver configuration directly from system settings with in-memory caching.
// ABOUTME: Keeps resolver bootstrapping independent from tenant-aware settings resolution and invalidates cache on updates.

using System.Text.Json;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Domain;
using Explore.Domain.Constants;
using Microsoft.Extensions.Caching.Memory;

namespace Explore.Infrastructure.Services;

public class ResolverConfigService : IResolverConfigService
{
    private const string CacheKey = "ResolverConfigService.Configuration";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly IMemoryCache _cache;

    public ResolverConfigService(
        ISystemSettingRepository systemSettingRepository,
        IMemoryCache cache)
    {
        _systemSettingRepository = systemSettingRepository;
        _cache = cache;
    }

    public async Task<ResolverConfigurationDto> GetConfigurationAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out ResolverConfigurationDto? cached) && cached != null)
        {
            return cached;
        }

        var configuration = new ResolverConfigurationDto
        {
            HeaderEnabled = DeserializeBoolean((await _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingResolverHeaderEnabled))?.Value, true),
            SubdomainEnabled = DeserializeBoolean((await _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingResolverSubdomainEnabled))?.Value, false),
            CustomDomainEnabled = DeserializeBoolean((await _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingResolverCustomDomainEnabled))?.Value, false),
            PathEnabled = DeserializeBoolean((await _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingResolverPathEnabled))?.Value, true),
            PathPrefix = NormalizePathPrefix(DeserializeString((await _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingPathPrefix))?.Value, "/t")),
            InstanceBaseDomain = NormalizeHost(DeserializeString((await _systemSettingRepository.GetByKey(GovernanceSettingKeys.DomainsInstanceBaseDomain))?.Value, string.Empty)),
            AllowTenantCustomDomains = DeserializeBoolean((await _systemSettingRepository.GetByKey(GovernanceSettingKeys.DomainsAllowTenantCustomDomain))?.Value, true)
        };

        configuration.HeaderEnabled = true;
        _cache.Set(CacheKey, configuration, CacheDuration);
        return configuration;
    }

    public async Task ApplyConfigurationAsync(ResolverConfigurationDto configuration, Guid? actorUserId, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(configuration);

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.RoutingResolverHeaderEnabled,
            JsonSerializer.Serialize(normalized.HeaderEnabled),
            SettingValueType.Boolean,
            true,
            "Routing",
            20,
            "Whether the header tenant resolver is enabled. Must remain enabled for YARP propagation.",
            actorUserId);

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.RoutingResolverCustomDomainEnabled,
            JsonSerializer.Serialize(normalized.CustomDomainEnabled),
            SettingValueType.Boolean,
            false,
            "Routing",
            21,
            "Whether the custom-domain tenant resolver is enabled.",
            actorUserId);

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.RoutingResolverSubdomainEnabled,
            JsonSerializer.Serialize(normalized.SubdomainEnabled),
            SettingValueType.Boolean,
            false,
            "Routing",
            22,
            "Whether the subdomain tenant resolver is enabled.",
            actorUserId);

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.RoutingResolverPathEnabled,
            JsonSerializer.Serialize(normalized.PathEnabled),
            SettingValueType.Boolean,
            false,
            "Routing",
            23,
            "Whether the path-based tenant resolver is enabled.",
            actorUserId);

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.RoutingPathPrefix,
            JsonSerializer.Serialize(normalized.PathPrefix),
            SettingValueType.String,
            false,
            "Routing",
            24,
            "Path prefix used by the path-based tenant resolver.",
            actorUserId);

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.DomainsInstanceBaseDomain,
            JsonSerializer.Serialize(normalized.InstanceBaseDomain),
            SettingValueType.String,
            false,
            "Domains",
            1,
            "Base domain used for tenant subdomain generation.",
            actorUserId);

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.DomainsAllowTenantCustomDomain,
            JsonSerializer.Serialize(normalized.AllowTenantCustomDomains),
            SettingValueType.Boolean,
            false,
            "Domains",
            2,
            "Whether tenant administrators may configure a custom domain.",
            actorUserId);

        InvalidateCache();
    }

    public void InvalidateCache()
    {
        _cache.Remove(CacheKey);
    }

    private async Task UpsertSystemSettingAsync(
        string settingKey,
        string value,
        SettingValueType valueType,
        bool isLocked,
        string category,
        int displayOrder,
        string description,
        Guid? actorUserId)
    {
        var existing = await _systemSettingRepository.GetByKey(settingKey);
        if (existing == null)
        {
            await _systemSettingRepository.Create(new SystemSetting
            {
                SettingKey = settingKey,
                Value = value,
                ValueType = valueType,
                IsLocked = isLocked,
                Description = description,
                Category = category,
                DisplayOrder = displayOrder,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = actorUserId
            });

            return;
        }

        existing.Value = value;
        existing.ValueType = valueType;
        existing.IsLocked = isLocked;
        existing.Description = description;
        existing.Category = category;
        existing.DisplayOrder = displayOrder;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = actorUserId;
        await _systemSettingRepository.Update(existing);
    }

    private static ResolverConfigurationDto Normalize(ResolverConfigurationDto configuration)
    {
        return new ResolverConfigurationDto
        {
            HeaderEnabled = true,
            SubdomainEnabled = configuration.SubdomainEnabled,
            CustomDomainEnabled = configuration.CustomDomainEnabled,
            PathEnabled = configuration.PathEnabled,
            PathPrefix = NormalizePathPrefix(configuration.PathPrefix),
            InstanceBaseDomain = NormalizeHost(configuration.InstanceBaseDomain),
            AllowTenantCustomDomains = configuration.AllowTenantCustomDomains
        };
    }

    private static string NormalizePathPrefix(string? pathPrefix)
    {
        if (string.IsNullOrWhiteSpace(pathPrefix))
        {
            return "/t";
        }

        var normalized = pathPrefix.Trim();
        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized;
        }

        return normalized.TrimEnd('/');
    }

    private static string NormalizeHost(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sanitized = value.Trim().ToLowerInvariant();
        sanitized = sanitized.Replace("https://", string.Empty, StringComparison.OrdinalIgnoreCase);
        sanitized = sanitized.Replace("http://", string.Empty, StringComparison.OrdinalIgnoreCase);
        return sanitized.Trim('/');
    }

    private static bool DeserializeBoolean(string? rawValue, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return defaultValue;
        }

        try
        {
            return JsonSerializer.Deserialize<bool>(rawValue);
        }
        catch
        {
            return bool.TryParse(rawValue, out var parsed) ? parsed : defaultValue;
        }
    }

    private static string DeserializeString(string? rawValue, string defaultValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return defaultValue;
        }

        try
        {
            var deserialized = JsonSerializer.Deserialize<string>(rawValue);
            return string.IsNullOrWhiteSpace(deserialized) ? defaultValue : deserialized;
        }
        catch
        {
            return rawValue.Trim('"');
        }
    }
}

// ABOUTME: Resolves analytics configuration from the cascading settings engine with short-lived cache.
// ABOUTME: Uses system defaults with tenant overrides and lock semantics through ISettingsResolver.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Analytics;

/// <summary>
/// Resolves analytics settings from the cascading settings engine (SystemSetting -> TenantSetting).
/// <para>
/// SaaS scenarios supported:
/// - Instance admin locks analytics settings -> all tenants use the SaaS provider's analytics
/// - Instance admin leaves analytics unlocked -> tenants can bring their own analytics provider
/// - Default analytics at instance level -> tenants use it unless they override
/// </para>
/// </summary>
public class AnalyticsConfigResolver : IAnalyticsConfigResolver
{
    private readonly ISettingsResolver _settingsResolver;
    private readonly ITenantContext _tenantContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AnalyticsConfigResolver> _logger;

    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "AnalyticsConfig:";

    public AnalyticsConfigResolver(
        ISettingsResolver settingsResolver,
        ITenantContext tenantContext,
        IMemoryCache cache,
        ILogger<AnalyticsConfigResolver> logger)
    {
        _settingsResolver = settingsResolver;
        _tenantContext = tenantContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<AnalyticsConfiguration> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var cacheKey = $"{CacheKeyPrefix}{tenantId}";

        if (_cache.TryGetValue(cacheKey, out AnalyticsConfiguration? cached) && cached is not null)
        {
            return cached;
        }

        var config = await ResolveFromSettingsAsync(tenantId, cancellationToken);
        _cache.Set(cacheKey, config, CacheExpiration);
        return config;
    }

    public void InvalidateCache(Guid? tenantId = null)
    {
        if (tenantId.HasValue)
        {
            _cache.Remove($"{CacheKeyPrefix}{tenantId.Value}");
            return;
        }

        _logger.LogInformation("Analytics config cache invalidation requested for all tenants");
    }

    private async Task<AnalyticsConfiguration> ResolveFromSettingsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        // ISettingsResolver handles the cascade:
        // 1. If setting is IsLocked at system level -> uses system value (instance admin control)
        // 2. If tenant has an override -> uses tenant value (tenant chooses own provider)
        // 3. Falls back to system default

        var providerStr = await _settingsResolver.GetSettingAsync<string>(GovernanceSettingKeys.AnalyticsProvider, tenantId, cancellationToken);
        var enabled = await _settingsResolver.GetSettingAsync<bool>(GovernanceSettingKeys.AnalyticsEnabled, tenantId, cancellationToken);
        var apiKey = await _settingsResolver.GetSettingAsync<string>(GovernanceSettingKeys.AnalyticsApiKey, tenantId, cancellationToken);
        var endpointUrl = await _settingsResolver.GetSettingAsync<string>(GovernanceSettingKeys.AnalyticsEndpointUrl, tenantId, cancellationToken);
        var personalApiKey = await _settingsResolver.GetSettingAsync<string>(GovernanceSettingKeys.AnalyticsPersonalApiKey, tenantId, cancellationToken);

        var provider = ParseProvider(providerStr);

        _logger.LogDebug("Analytics config resolved for tenant {TenantId}: Provider={Provider}, Enabled={Enabled}",
            tenantId, provider, enabled);

        return new AnalyticsConfiguration
        {
            Provider = provider,
            IsEnabled = enabled,
            ApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey,
            EndpointUrl = string.IsNullOrWhiteSpace(endpointUrl) ? null : endpointUrl,
            PersonalApiKey = string.IsNullOrWhiteSpace(personalApiKey) ? null : personalApiKey
        };
    }

    private static AnalyticsProviderEnum ParseProvider(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "posthog" => AnalyticsProviderEnum.Posthog,
            "plausible" => AnalyticsProviderEnum.Plausible,
            "rybbit" => AnalyticsProviderEnum.Rybbit,
            "rudderstack" => AnalyticsProviderEnum.RudderStack,
            _ => AnalyticsProviderEnum.None
        };
    }
}

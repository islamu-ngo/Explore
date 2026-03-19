// ABOUTME: Resolves analytics configuration from the hierarchical settings engine with short-lived cache.
// ABOUTME: Uses system defaults with tenant overrides and lock semantics through IHierarchicalSettingsResolver.

using Explore.Application.Analytics;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models;
using Explore.Application.Settings;
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
    private readonly IHierarchicalSettingsResolver _resolver;
    private readonly ITenantContext _tenantContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AnalyticsConfigResolver> _logger;

    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "AnalyticsConfig:";

    public AnalyticsConfigResolver(
        IHierarchicalSettingsResolver resolver,
        ITenantContext tenantContext,
        IMemoryCache cache,
        ILogger<AnalyticsConfigResolver> logger)
    {
        _resolver = resolver;
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
        // IHierarchicalSettingsResolver handles the cascade:
        // 1. If setting is IsLocked at system level -> uses system value (instance admin control)
        // 2. If tenant has an override -> uses tenant value (tenant chooses own provider)
        // 3. Falls back to system default

        var ctx = new SettingContext(TenantId: tenantId);

        var providerStr = await _resolver.ResolveAsync<string>(GovernanceSettingKeys.Analytics.Provider, ctx, cancellationToken);
        var enabled = await _resolver.ResolveAsync<bool>(GovernanceSettingKeys.Analytics.Enabled, ctx, cancellationToken);
        var consentMode = await _resolver.ResolveAsync<string>(GovernanceSettingKeys.Analytics.ConsentMode, ctx, cancellationToken);
        var transportMode = await _resolver.ResolveAsync<string>(GovernanceSettingKeys.Analytics.TransportMode, ctx, cancellationToken);
        var apiKey = await _resolver.ResolveAsync<string>(GovernanceSettingKeys.Analytics.ApiKey, ctx, cancellationToken);
        var endpointUrl = await _resolver.ResolveAsync<string>(GovernanceSettingKeys.Analytics.EndpointUrl, ctx, cancellationToken);
        var personalApiKey = await _resolver.ResolveAsync<string>(GovernanceSettingKeys.Analytics.PersonalApiKey, ctx, cancellationToken);

        var provider = ParseProvider(providerStr);

        _logger.LogDebug("Analytics config resolved for tenant {TenantId}: Provider={Provider}, Enabled={Enabled}",
            tenantId, provider, enabled);

        return new AnalyticsConfiguration
        {
            Provider = provider,
            IsEnabled = enabled,
            ConsentMode = ParseConsentMode(consentMode),
            TransportMode = ParseTransportMode(transportMode),
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

    private static AnalyticsConsentMode ParseConsentMode(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "anonymous" => AnalyticsConsentMode.Anonymous,
            "identified" => AnalyticsConsentMode.Identified,
            _ => AnalyticsConsentMode.Pseudonymous
        };
    }

    private static AnalyticsTransportMode ParseTransportMode(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "proxy" => AnalyticsTransportMode.Proxy,
            "relay" => AnalyticsTransportMode.Relay,
            _ => AnalyticsTransportMode.Direct
        };
    }
}

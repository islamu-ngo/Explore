// ABOUTME: Resolves Cerbos PDP configuration from the cascading settings engine.
// Supports BYO (Bring Your Own) Cerbos per tenant and instance-managed scope isolation.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models;
using Explore.Domain.Constants;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Services;

/// <summary>
/// Resolves Cerbos PDP settings from the cascading settings engine (SystemSetting → TenantSetting).
/// <para>
/// SaaS scenarios supported:
/// - Instance admin locks Cerbos settings → all tenants use the instance PDP
/// - Instance admin enables customization → tenants can bring their own Cerbos PDP
/// - Default: all tenants use instance PDP with scope-based policy isolation
/// </para>
/// </summary>
public class CerbosConfigResolver : ICerbosConfigResolver
{
    private readonly ISettingsResolver _settingsResolver;
    private readonly ITenantContext _tenantContext;
    private readonly IMemoryCache _cache;
    private readonly CerbosSettings _instanceSettings;
    private readonly ILogger<CerbosConfigResolver> _logger;

    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "CerbosConfig:";

    public CerbosConfigResolver(
        ISettingsResolver settingsResolver,
        ITenantContext tenantContext,
        IMemoryCache cache,
        IOptions<CerbosSettings> instanceSettings,
        ILogger<CerbosConfigResolver> logger)
    {
        _settingsResolver = settingsResolver;
        _tenantContext = tenantContext;
        _cache = cache;
        _instanceSettings = instanceSettings.Value;
        _logger = logger;
    }

    public async Task<CerbosConfiguration?> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var cacheKey = $"{CacheKeyPrefix}{tenantId}";

        if (_cache.TryGetValue(cacheKey, out CerbosConfiguration? cached))
            return cached;

        var config = await ResolveFromSettingsAsync(tenantId, cancellationToken);

        if (config is not null)
            _cache.Set(cacheKey, config, CacheExpiration);

        return config;
    }

    public void InvalidateCache(Guid? tenantId = null)
    {
        if (tenantId.HasValue)
        {
            _cache.Remove($"{CacheKeyPrefix}{tenantId.Value}");
        }
        else
        {
            _logger.LogInformation("Cerbos config cache invalidation requested for all tenants");
        }
    }

    private async Task<CerbosConfiguration?> ResolveFromSettingsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        // Check if tenant customization is enabled at instance level.
        // When disabled (or locked), all tenants use the instance PDP.
        var customizationEnabled = await _settingsResolver.GetSettingAsync<bool>(
            GovernanceSettingKeys.Cerbos.TenantCustomizationEnabled, tenantId, cancellationToken);

        if (!customizationEnabled)
            return BuildInstanceDefault();

        // Resolve per-tenant Cerbos mode
        var modeStr = await _settingsResolver.GetSettingAsync<string>(
            GovernanceSettingKeys.Cerbos.Mode, tenantId, cancellationToken);

        var mode = ParseCerbosMode(modeStr);

        if (mode == CerbosMode.Instance)
            return BuildInstanceDefault();

        // BYO: resolve custom endpoint
        var customEndpoint = await _settingsResolver.GetSettingAsync<string>(
            GovernanceSettingKeys.Cerbos.CustomEndpoint, tenantId, cancellationToken);

        if (string.IsNullOrWhiteSpace(customEndpoint))
        {
            _logger.LogWarning(
                "Tenant {TenantId} has cerbos.mode=custom_endpoint but no custom endpoint configured. Falling back to instance PDP",
                tenantId);
            return BuildInstanceDefault();
        }

        // Resolve failure mode
        var failureModeStr = await _settingsResolver.GetSettingAsync<string>(
            GovernanceSettingKeys.Cerbos.FailureMode, tenantId, cancellationToken);
        var failureMode = ParseFailureMode(failureModeStr);

        // Resolve optional Admin API config
        var adminEndpoint = await _settingsResolver.GetSettingAsync<string>(
            GovernanceSettingKeys.Cerbos.CustomAdminEndpoint, tenantId, cancellationToken);
        var adminUsername = await _settingsResolver.GetSettingAsync<string>(
            InfrastructureSecretSettingKeys.Cerbos.CustomAdminUsername, tenantId, cancellationToken);
        var adminPassword = await _settingsResolver.GetSettingAsync<string>(
            InfrastructureSecretSettingKeys.Cerbos.CustomAdminPassword, tenantId, cancellationToken);

        _logger.LogDebug(
            "Resolved BYO Cerbos for tenant {TenantId}: endpoint={Endpoint}, failureMode={FailureMode}",
            tenantId, customEndpoint, failureMode);

        return new CerbosConfiguration
        {
            Endpoint = customEndpoint,
            Mode = CerbosMode.CustomEndpoint,
            FailureMode = failureMode,
            AdminEndpoint = string.IsNullOrWhiteSpace(adminEndpoint) ? null : adminEndpoint,
            AdminUsername = string.IsNullOrWhiteSpace(adminUsername) ? null : adminUsername,
            AdminPassword = string.IsNullOrWhiteSpace(adminPassword) ? null : adminPassword,
            IsInstanceDefault = false
        };
    }

    private CerbosConfiguration BuildInstanceDefault()
    {
        return new CerbosConfiguration
        {
            Endpoint = _instanceSettings.Endpoint,
            Mode = CerbosMode.Instance,
            FailureMode = CerbosFailureMode.Open,
            IsInstanceDefault = true
        };
    }

    private static CerbosMode ParseCerbosMode(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "custom_endpoint" => CerbosMode.CustomEndpoint,
            _ => CerbosMode.Instance
        };

    private static CerbosFailureMode ParseFailureMode(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "open" => CerbosFailureMode.Open,
            _ => CerbosFailureMode.Closed
        };
}

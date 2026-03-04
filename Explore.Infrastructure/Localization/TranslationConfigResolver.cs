// ABOUTME: Resolves TMS configuration from the cascading settings engine with short-lived cache.
// ABOUTME: Uses system defaults with tenant overrides through ISettingsResolver (mirrors AnalyticsConfigResolver).

using Explore.Application.Contracts.Infrastructure;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Localization;

/// <summary>
/// Resolves translation settings from the cascading settings engine (SystemSetting → TenantSetting).
/// </summary>
public class TranslationConfigResolver : ITranslationConfigResolver
{
    private readonly ISettingsResolver _settingsResolver;
    private readonly ITenantContext _tenantContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TranslationConfigResolver> _logger;

    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "TranslationConfig:";

    public TranslationConfigResolver(
        ISettingsResolver settingsResolver,
        ITenantContext tenantContext,
        IMemoryCache cache,
        ILogger<TranslationConfigResolver> logger)
    {
        _settingsResolver = settingsResolver;
        _tenantContext = tenantContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<TranslationConfiguration> ResolveAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var cacheKey = $"{CacheKeyPrefix}{tenantId}";

        if (_cache.TryGetValue(cacheKey, out TranslationConfiguration? cached) && cached is not null)
        {
            return cached;
        }

        var config = await ResolveFromSettingsAsync(tenantId, ct);
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

        _logger.LogInformation("Translation config cache invalidation requested for all tenants");
    }

    private async Task<TranslationConfiguration> ResolveFromSettingsAsync(Guid tenantId, CancellationToken ct)
    {
        var providerStr = await _settingsResolver.GetSettingAsync<string>(GovernanceSettingKeys.LocalizationTmsProvider, tenantId, ct);
        var apiUrl = await _settingsResolver.GetSettingAsync<string>(GovernanceSettingKeys.LocalizationTmsApiUrl, tenantId, ct);
        var projectId = await _settingsResolver.GetSettingAsync<string>(GovernanceSettingKeys.LocalizationTmsProjectId, tenantId, ct);
        var component = await _settingsResolver.GetSettingAsync<string>(GovernanceSettingKeys.LocalizationTmsComponent, tenantId, ct);
        var defaultLanguage = await _settingsResolver.GetSettingAsync<string>(GovernanceSettingKeys.LocalizationDefaultLanguage, tenantId, ct);

        var provider = ParseProvider(providerStr);

        _logger.LogDebug("Translation config resolved for tenant {TenantId}: Provider={Provider}, DefaultLanguage={DefaultLanguage}",
            tenantId, provider, defaultLanguage ?? "en");

        return new TranslationConfiguration(
            Provider: provider,
            ApiUrl: string.IsNullOrWhiteSpace(apiUrl) ? null : apiUrl,
            ProjectId: string.IsNullOrWhiteSpace(projectId) ? null : projectId,
            Component: string.IsNullOrWhiteSpace(component) ? null : component,
            DefaultLanguage: string.IsNullOrWhiteSpace(defaultLanguage) ? "en" : defaultLanguage
        );
    }

    private static TranslationManagementProviderEnum ParseProvider(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "tolgee" => TranslationManagementProviderEnum.Tolgee,
            "weblate" => TranslationManagementProviderEnum.Weblate,
            _ => TranslationManagementProviderEnum.None
        };
    }
}

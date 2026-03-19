// ABOUTME: Resolves TMS configuration from the hierarchical settings engine with short-lived cache.
// ABOUTME: Uses system defaults with tenant overrides through IHierarchicalSettingsResolver.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Settings;
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
    private readonly IHierarchicalSettingsResolver _resolver;
    private readonly ITenantContext _tenantContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TranslationConfigResolver> _logger;

    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "TranslationConfig:";

    public TranslationConfigResolver(
        IHierarchicalSettingsResolver resolver,
        ITenantContext tenantContext,
        IMemoryCache cache,
        ILogger<TranslationConfigResolver> logger)
    {
        _resolver = resolver;
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
        var ctx = new SettingContext(TenantId: tenantId);

        var providerStr = await _resolver.ResolveAsync<string>(GovernanceSettingKeys.Localization.TmsProvider, ctx, ct);
        var apiUrl = await _resolver.ResolveAsync<string>(GovernanceSettingKeys.Localization.TmsApiUrl, ctx, ct);
        var projectId = await _resolver.ResolveAsync<string>(GovernanceSettingKeys.Localization.TmsProjectId, ctx, ct);
        var component = await _resolver.ResolveAsync<string>(GovernanceSettingKeys.Localization.TmsComponent, ctx, ct);
        var defaultLanguage = await _resolver.ResolveAsync<string>(GovernanceSettingKeys.Localization.DefaultLanguage, ctx, ct);

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

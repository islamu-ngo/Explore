// ABOUTME: Runtime TMS provider wrapper that resolves active provider from tenant settings at runtime.
// ABOUTME: None → OfflineTranslationProvider, Tolgee → TolgeeTranslationProvider, Weblate → WeblateTranslationProvider.

using Explore.Application.Contracts.Infrastructure;
using Explore.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Localization;

/// <summary>
/// Runtime-switchable TMS provider that delegates to the configured provider.
/// Falls back to OfflineTranslationProvider on errors (graceful degradation).
/// </summary>
public sealed class RuntimeTranslationProvider : ITranslationManagementProvider
{
    private readonly TolgeeTranslationProvider _tolgeeProvider;
    private readonly WeblateTranslationProvider _weblateProvider;
    private readonly OfflineTranslationProvider _offlineProvider;
    private readonly NullTranslationProvider _nullProvider;
    private readonly ITranslationConfigResolver _configResolver;
    private readonly ILogger<RuntimeTranslationProvider> _logger;

    public RuntimeTranslationProvider(
        TolgeeTranslationProvider tolgeeProvider,
        WeblateTranslationProvider weblateProvider,
        OfflineTranslationProvider offlineProvider,
        NullTranslationProvider nullProvider,
        ITranslationConfigResolver configResolver,
        ILogger<RuntimeTranslationProvider> logger)
    {
        _tolgeeProvider = tolgeeProvider;
        _weblateProvider = weblateProvider;
        _offlineProvider = offlineProvider;
        _nullProvider = nullProvider;
        _configResolver = configResolver;
        _logger = logger;
    }

    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        var provider = await ResolveProviderAsync(ct);
        try
        {
            return await provider.TestConnectionAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TMS TestConnection failed on {Provider}; falling back to OfflineProvider", provider.GetType().Name);
            return await _offlineProvider.TestConnectionAsync(ct);
        }
    }

    public async Task ImportKeysAsync(IEnumerable<TranslationKeyImport> keys, CancellationToken ct = default)
    {
        var provider = await ResolveProviderAsync(ct);
        try
        {
            await provider.ImportKeysAsync(keys, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TMS ImportKeys failed on {Provider}", provider.GetType().Name);
        }
    }

    public async Task<IEnumerable<TranslationExport>> ExportTranslationsAsync(string languageCode, CancellationToken ct = default)
    {
        var provider = await ResolveProviderAsync(ct);
        try
        {
            var results = await provider.ExportTranslationsAsync(languageCode, ct);
            var list = results.ToList();
            if (list.Count > 0) return list;

            // Live provider returned empty — try offline as fallback
            if (provider != _offlineProvider)
            {
                _logger.LogDebug("Live TMS returned empty for {Language}; trying offline bundles", languageCode);
                return await _offlineProvider.ExportTranslationsAsync(languageCode, ct);
            }

            return list;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TMS ExportTranslations failed on {Provider} for {Language}; falling back to OfflineProvider",
                provider.GetType().Name, languageCode);
            return await _offlineProvider.ExportTranslationsAsync(languageCode, ct);
        }
    }

    public async Task<IEnumerable<string>> GetAvailableLanguagesAsync(CancellationToken ct = default)
    {
        var provider = await ResolveProviderAsync(ct);
        try
        {
            return await provider.GetAvailableLanguagesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TMS GetAvailableLanguages failed on {Provider}; falling back to OfflineProvider", provider.GetType().Name);
            return await _offlineProvider.GetAvailableLanguagesAsync(ct);
        }
    }

    private async Task<ITranslationManagementProvider> ResolveProviderAsync(CancellationToken ct)
    {
        try
        {
            var config = await _configResolver.ResolveAsync(ct);

            return config.Provider switch
            {
                TranslationManagementProviderEnum.Tolgee => _tolgeeProvider,
                TranslationManagementProviderEnum.Weblate => _weblateProvider,
                TranslationManagementProviderEnum.None => _offlineProvider,
                _ => _offlineProvider
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve TMS provider; defaulting to OfflineProvider");
            return _offlineProvider;
        }
    }
}

// ABOUTME: Offline translation provider that reads pre-exported TMS JSON bundles as embedded resources.
// ABOUTME: Default provider when no TMS is configured — self-hosters always get translations from last build.

using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Localization;

/// <summary>
/// Reads flat key-value JSON bundles shipped as embedded resources.
/// Bundle format: { "lookup.tag.FIQH.full_name": "Jurisprudence islamique", ... }
/// One file per language (en.json, fr.json, ar.json).
/// </summary>
public class OfflineTranslationProvider : ITranslationManagementProvider
{
    private readonly ILogger<OfflineTranslationProvider> _logger;
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> _bundles = new();
    private readonly Assembly _bundleAssembly;
    private readonly string _bundlePrefix;
    private bool _languagesScanned;
    private readonly List<string> _availableLanguages = [];
    private readonly object _scanLock = new();

    public OfflineTranslationProvider(ILogger<OfflineTranslationProvider> logger)
    {
        _logger = logger;
        _bundleAssembly = typeof(OfflineTranslationProvider).Assembly;
        _bundlePrefix = $"{_bundleAssembly.GetName().Name}.Localization.Bundles.";
    }

    public Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        EnsureLanguagesScanned();
        return Task.FromResult(_availableLanguages.Count > 0);
    }

    public Task ImportKeysAsync(IEnumerable<TranslationKeyImport> keys, CancellationToken ct = default)
    {
        _logger.LogWarning("OfflineTranslationProvider does not support import — bundles are read-only");
        return Task.CompletedTask;
    }

    public Task<IEnumerable<TranslationExport>> ExportTranslationsAsync(string languageCode, CancellationToken ct = default)
    {
        var bundle = GetOrLoadBundle(languageCode);
        var exports = bundle.Select(kvp => new TranslationExport(kvp.Key, kvp.Value));
        return Task.FromResult(exports);
    }

    public Task<IEnumerable<string>> GetAvailableLanguagesAsync(CancellationToken ct = default)
    {
        EnsureLanguagesScanned();
        return Task.FromResult<IEnumerable<string>>(_availableLanguages);
    }

    private IReadOnlyDictionary<string, string> GetOrLoadBundle(string languageCode)
    {
        var normalizedCode = languageCode.ToLowerInvariant();
        return _bundles.GetOrAdd(normalizedCode, LoadBundle);
    }

    private IReadOnlyDictionary<string, string> LoadBundle(string languageCode)
    {
        var resourceName = $"{_bundlePrefix}{languageCode}.json";
        using var stream = _bundleAssembly.GetManifestResourceStream(resourceName);

        if (stream is null)
        {
            _logger.LogDebug("No offline bundle found for language {Language} (resource: {Resource})", languageCode, resourceName);
            return new Dictionary<string, string>();
        }

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
                       ?? new Dictionary<string, string>();
            _logger.LogInformation("Loaded offline translation bundle for {Language}: {Count} keys", languageCode, dict.Count);
            return dict;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse offline translation bundle for {Language}", languageCode);
            return new Dictionary<string, string>();
        }
    }

    private void EnsureLanguagesScanned()
    {
        if (_languagesScanned) return;

        lock (_scanLock)
        {
            if (_languagesScanned) return;

            var resourceNames = _bundleAssembly.GetManifestResourceNames();
            foreach (var name in resourceNames)
            {
                if (name.StartsWith(_bundlePrefix, StringComparison.OrdinalIgnoreCase) && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    var langCode = name[_bundlePrefix.Length..^5]; // strip prefix and .json
                    _availableLanguages.Add(langCode);
                }
            }

            _languagesScanned = true;
            _logger.LogInformation("Offline translation bundles scanned: {Count} languages found ({Languages})",
                _availableLanguages.Count, string.Join(", ", _availableLanguages));
        }
    }
}

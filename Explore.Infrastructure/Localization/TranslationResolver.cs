// ABOUTME: Unified translation resolver — single entry point for all translation needs.
// ABOUTME: Uses RuntimeTranslationProvider with MemoryCache for efficient batch resolution.

using System.Collections.Concurrent;
using Explore.Application.Contracts.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Localization;

/// <summary>
/// Resolves translations through the RuntimeTranslationProvider with caching.
/// <para>
/// Resolution chain:
/// 1. Check MemoryCache
/// 2. Ask RuntimeTranslationProvider (live TMS or offline bundles)
/// 3. If empty → return key itself as fallback
/// 4. Cache result (30-min TTL for live, app-lifetime for offline)
/// </para>
/// </summary>
public class TranslationResolver : ITranslationResolver
{
    private readonly RuntimeTranslationProvider _runtimeProvider;
    private readonly ITranslationConfigResolver _configResolver;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TranslationResolver> _logger;

    // Language → translations loaded flag (for batch preloading)
    private readonly ConcurrentDictionary<string, bool> _preloadedLanguages = new();

    private static readonly TimeSpan LiveCacheDuration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan OfflineCacheDuration = TimeSpan.FromHours(24);
    private const string CacheKeyPrefix = "Translation:";

    public TranslationResolver(
        RuntimeTranslationProvider runtimeProvider,
        ITranslationConfigResolver configResolver,
        IMemoryCache cache,
        ILogger<TranslationResolver> logger)
    {
        _runtimeProvider = runtimeProvider;
        _configResolver = configResolver;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> ResolveAsync(string key, string languageCode, CancellationToken ct = default)
    {
        var cacheKey = BuildCacheKey(key, languageCode);

        if (_cache.TryGetValue(cacheKey, out string? cached) && cached is not null)
        {
            return cached;
        }

        // Preload all translations for this language on first access
        await EnsureLanguagePreloadedAsync(languageCode, ct);

        // Check cache again after preload
        if (_cache.TryGetValue(cacheKey, out cached) && cached is not null)
        {
            return cached;
        }

        // Key not found in any bundle — return key itself as fallback
        return key;
    }

    public async Task<IDictionary<string, string>> ResolveBatchAsync(IEnumerable<string> keys, string languageCode, CancellationToken ct = default)
    {
        await EnsureLanguagePreloadedAsync(languageCode, ct);

        var result = new Dictionary<string, string>();
        foreach (var key in keys)
        {
            var cacheKey = BuildCacheKey(key, languageCode);
            if (_cache.TryGetValue(cacheKey, out string? cached) && cached is not null)
            {
                result[key] = cached;
            }
            else
            {
                result[key] = key; // fallback to key itself
            }
        }

        return result;
    }

    private async Task EnsureLanguagePreloadedAsync(string languageCode, CancellationToken ct)
    {
        var normalizedLang = languageCode.ToLowerInvariant();

        if (_preloadedLanguages.ContainsKey(normalizedLang))
            return;

        try
        {
            var exports = await _runtimeProvider.ExportTranslationsAsync(normalizedLang, ct);
            var exportList = exports.ToList();

            var config = await _configResolver.ResolveAsync(ct);
            var cacheDuration = config.Provider == Domain.Enums.TranslationManagementProviderEnum.None
                ? OfflineCacheDuration
                : LiveCacheDuration;

            foreach (var export in exportList)
            {
                var cacheKey = BuildCacheKey(export.KeyName, normalizedLang);
                _cache.Set(cacheKey, export.Value, cacheDuration);
            }

            _preloadedLanguages.TryAdd(normalizedLang, true);
            _logger.LogInformation("Preloaded {Count} translations for language {Language} (TTL: {TTL})",
                exportList.Count, normalizedLang, cacheDuration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to preload translations for {Language}", normalizedLang);
            _preloadedLanguages.TryAdd(normalizedLang, true); // prevent retry storms
        }
    }

    private static string BuildCacheKey(string key, string languageCode)
        => $"{CacheKeyPrefix}{languageCode.ToLowerInvariant()}:{key}";
}

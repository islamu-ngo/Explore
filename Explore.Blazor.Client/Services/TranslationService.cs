// ABOUTME: Client-side translation service with in-memory caching (30-min TTL).
// ABOUTME: Fetches translations from API via NSwag client, provides T(key) accessor with key-as-fallback.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

public class TranslationService : ITranslationService, IDisposable
{
    private record CacheEntry<T>(T Data, DateTime ExpiresAt)
    {
        public bool IsValid => DateTime.UtcNow < ExpiresAt;
    }

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    private readonly IEventApiClient _apiClient;
    private readonly ILogger<TranslationService> _logger;
    private readonly SemaphoreSlim _translationLock = new(1, 1);
    private readonly SemaphoreSlim _languagesLock = new(1, 1);

    private CacheEntry<IDictionary<string, string>>? _translationsCache;
    private CacheEntry<ICollection<string>>? _languagesCache;
    private string _currentLanguage = "en";

    public string CurrentLanguage => _currentLanguage;

    public event Action<string>? OnLanguageChanged;

    public TranslationService(IEventApiClient apiClient, ILogger<TranslationService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public string T(string key, string? fallback = null)
    {
        if (string.IsNullOrEmpty(key))
            return fallback ?? string.Empty;

        var cache = _translationsCache;
        if (cache is { IsValid: true } && cache.Data.TryGetValue(key, out var value))
            return value;

        return fallback ?? key;
    }

    public async Task<IDictionary<string, string>> GetTranslationsAsync(string languageCode, CancellationToken ct = default)
    {
        var cache = _translationsCache;
        if (cache is { IsValid: true } && string.Equals(_currentLanguage, languageCode, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("[TRANSLATION] Cache hit for {Language}", languageCode);
            return cache.Data;
        }

        await _translationLock.WaitAsync(ct);
        try
        {
            cache = _translationsCache;
            if (cache is { IsValid: true } && string.Equals(_currentLanguage, languageCode, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("[TRANSLATION] Cache hit for {Language} (after lock)", languageCode);
                return cache.Data;
            }

            _logger.LogDebug("[TRANSLATION] Cache miss for {Language}, fetching from API", languageCode);
            var translations = await _apiClient.TranslationAsync(languageCode, ct);
            _translationsCache = new CacheEntry<IDictionary<string, string>>(translations, DateTime.UtcNow.Add(CacheDuration));
            return translations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch translations for {Language}", languageCode);
            return _translationsCache?.Data ?? new Dictionary<string, string>();
        }
        finally
        {
            _translationLock.Release();
        }
    }

    public async Task<ICollection<string>> GetAvailableLanguagesAsync(CancellationToken ct = default)
    {
        var cache = _languagesCache;
        if (cache is { IsValid: true })
        {
            _logger.LogDebug("[TRANSLATION] Languages cache hit");
            return cache.Data;
        }

        await _languagesLock.WaitAsync(ct);
        try
        {
            cache = _languagesCache;
            if (cache is { IsValid: true })
                return cache.Data;

            _logger.LogDebug("[TRANSLATION] Languages cache miss, fetching from API");
            var languages = await _apiClient.LanguagesAsync(ct);
            _languagesCache = new CacheEntry<ICollection<string>>(languages, DateTime.UtcNow.Add(CacheDuration));
            return languages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch available languages");
            return _languagesCache?.Data ?? new List<string> { "en" };
        }
        finally
        {
            _languagesLock.Release();
        }
    }

    public async Task ChangeLanguageAsync(string languageCode, CancellationToken ct = default)
    {
        if (string.Equals(_currentLanguage, languageCode, StringComparison.OrdinalIgnoreCase))
            return;

        _logger.LogInformation("[TRANSLATION] Changing language from {Old} to {New}", _currentLanguage, languageCode);

        _translationsCache = null;
        _currentLanguage = languageCode;

        await GetTranslationsAsync(languageCode, ct);
        OnLanguageChanged?.Invoke(languageCode);
    }

    public async Task PreloadAsync(string languageCode, CancellationToken ct = default)
    {
        _currentLanguage = languageCode;
        await GetTranslationsAsync(languageCode, ct);
    }

    public void Dispose()
    {
        _translationLock.Dispose();
        _languagesLock.Dispose();
    }
}

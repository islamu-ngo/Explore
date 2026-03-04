// ABOUTME: Tolgee TMS provider implementation using Tolgee REST API v2.
// ABOUTME: Supports import, export, language listing with X-API-Key authentication.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Localization;

/// <summary>
/// Tolgee Translation Management System provider.
/// API docs: https://tolgee.io/api
/// Auth: X-API-Key header.
/// </summary>
public class TolgeeTranslationProvider : ITranslationManagementProvider
{
    private readonly HttpClient _httpClient;
    private readonly ITranslationConfigResolver _configResolver;
    private readonly ILogger<TolgeeTranslationProvider> _logger;

    public TolgeeTranslationProvider(
        HttpClient httpClient,
        ITranslationConfigResolver configResolver,
        ILogger<TolgeeTranslationProvider> logger)
    {
        _httpClient = httpClient;
        _configResolver = configResolver;
        _logger = logger;
    }

    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        var config = await _configResolver.ResolveAsync(ct);
        if (string.IsNullOrWhiteSpace(config.ApiUrl) || string.IsNullOrWhiteSpace(config.ProjectId))
            return false;

        try
        {
            var request = CreateRequest(HttpMethod.Get, config, $"/v2/projects/{config.ProjectId}");
            var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tolgee connection test failed");
            return false;
        }
    }

    public async Task ImportKeysAsync(IEnumerable<TranslationKeyImport> keys, CancellationToken ct = default)
    {
        var config = await _configResolver.ResolveAsync(ct);
        if (string.IsNullOrWhiteSpace(config.ApiUrl) || string.IsNullOrWhiteSpace(config.ProjectId))
        {
            _logger.LogWarning("Tolgee import skipped: missing ApiUrl or ProjectId");
            return;
        }

        var keyList = keys.ToList();
        if (keyList.Count == 0) return;

        // Tolgee batch key create/update endpoint
        var payload = new
        {
            keys = keyList.Select(k => new
            {
                name = k.KeyName,
                translations = k.Translations
            })
        };

        var request = CreateRequest(HttpMethod.Post, config, $"/v2/projects/{config.ProjectId}/keys/import-resolvable");
        request.Content = JsonContent.Create(payload);

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Tolgee import failed with {StatusCode}: {Body}", response.StatusCode, body);
        }
        else
        {
            _logger.LogInformation("Tolgee import completed: {Count} keys", keyList.Count);
        }
    }

    public async Task<IEnumerable<TranslationExport>> ExportTranslationsAsync(string languageCode, CancellationToken ct = default)
    {
        var config = await _configResolver.ResolveAsync(ct);
        if (string.IsNullOrWhiteSpace(config.ApiUrl) || string.IsNullOrWhiteSpace(config.ProjectId))
            return [];

        var request = CreateRequest(HttpMethod.Get, config,
            $"/v2/projects/{config.ProjectId}/translations/{languageCode}?structureDelimiter=.");

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Tolgee export failed for {Language}: {StatusCode}", languageCode, response.StatusCode);
            return [];
        }

        try
        {
            using var stream = await response.Content.ReadAsStreamAsync(ct);
            var tolgeeResponse = await JsonSerializer.DeserializeAsync<TolgeeTranslationsResponse>(stream, cancellationToken: ct);

            if (tolgeeResponse?._embedded?.Keys is null)
                return [];

            var exports = new List<TranslationExport>();
            foreach (var key in tolgeeResponse._embedded.Keys)
            {
                if (key.Translations.TryGetValue(languageCode, out var translation) && translation?.Text is not null)
                {
                    exports.Add(new TranslationExport(key.KeyName, translation.Text));
                }
            }

            return exports;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Tolgee response for {Language}", languageCode);
            return [];
        }
    }

    public async Task<IEnumerable<string>> GetAvailableLanguagesAsync(CancellationToken ct = default)
    {
        var config = await _configResolver.ResolveAsync(ct);
        if (string.IsNullOrWhiteSpace(config.ApiUrl) || string.IsNullOrWhiteSpace(config.ProjectId))
            return [];

        var request = CreateRequest(HttpMethod.Get, config, $"/v2/projects/{config.ProjectId}/languages");
        var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Tolgee list languages failed: {StatusCode}", response.StatusCode);
            return [];
        }

        try
        {
            using var stream = await response.Content.ReadAsStreamAsync(ct);
            var langResponse = await JsonSerializer.DeserializeAsync<TolgeeLanguagesResponse>(stream, cancellationToken: ct);
            return langResponse?._embedded?.Languages?.Select(l => l.Tag) ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Tolgee languages response");
            return [];
        }
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, TranslationConfiguration config, string path)
    {
        var request = new HttpRequestMessage(method, $"{config.ApiUrl!.TrimEnd('/')}{path}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        // API key is set on the HttpClient via DI configuration (SecretProvider)
        return request;
    }

    // Tolgee API response models (internal)
    private sealed record TolgeeTranslationsResponse(TolgeeTranslationsEmbedded? _embedded);
    private sealed record TolgeeTranslationsEmbedded(List<TolgeeKeyWithTranslation>? Keys);
    private sealed record TolgeeKeyWithTranslation(string KeyName, Dictionary<string, TolgeeTranslationValue> Translations);
    private sealed record TolgeeTranslationValue(string? Text);
    private sealed record TolgeeLanguagesResponse(TolgeeLanguagesEmbedded? _embedded);
    private sealed record TolgeeLanguagesEmbedded(List<TolgeeLanguage>? Languages);
    private sealed record TolgeeLanguage(string Tag);
}

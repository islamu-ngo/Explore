// ABOUTME: Weblate TMS provider implementation using Weblate REST API.
// ABOUTME: Supports import, export, language listing with Token authentication.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Localization;

/// <summary>
/// Weblate Translation Management System provider.
/// API docs: https://docs.weblate.org/en/latest/api.html
/// Auth: Authorization: Token {token} header.
/// </summary>
public class WeblateTranslationProvider : ITranslationManagementProvider
{
    private readonly HttpClient _httpClient;
    private readonly ITranslationConfigResolver _configResolver;
    private readonly ILogger<WeblateTranslationProvider> _logger;

    public WeblateTranslationProvider(
        HttpClient httpClient,
        ITranslationConfigResolver configResolver,
        ILogger<WeblateTranslationProvider> logger)
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
            var request = CreateRequest(HttpMethod.Get, config, $"/api/projects/{config.ProjectId}/");
            var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Weblate connection test failed");
            return false;
        }
    }

    public async Task ImportKeysAsync(IEnumerable<TranslationKeyImport> keys, CancellationToken ct = default)
    {
        var config = await _configResolver.ResolveAsync(ct);
        if (string.IsNullOrWhiteSpace(config.ApiUrl) || string.IsNullOrWhiteSpace(config.ProjectId) || string.IsNullOrWhiteSpace(config.Component))
        {
            _logger.LogWarning("Weblate import skipped: missing ApiUrl, ProjectId, or Component");
            return;
        }

        var keyList = keys.ToList();
        if (keyList.Count == 0) return;

        // Weblate requires per-language unit creation
        var languageCodes = keyList.SelectMany(k => k.Translations.Keys).Distinct().ToList();

        foreach (var lang in languageCodes)
        {
            foreach (var key in keyList)
            {
                if (!key.Translations.TryGetValue(lang, out var translation)) continue;

                var payload = new
                {
                    key = key.KeyName,
                    value = new[] { translation }
                };

                var request = CreateRequest(HttpMethod.Post, config,
                    $"/api/translations/{config.ProjectId}/{config.Component}/{lang}/units/");
                request.Content = JsonContent.Create(payload);

                var response = await _httpClient.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogDebug("Weblate unit create for {Key}/{Lang}: {Status} {Body}", key.KeyName, lang, response.StatusCode, body);
                }
            }
        }

        _logger.LogInformation("Weblate import completed: {Count} keys across {LangCount} languages", keyList.Count, languageCodes.Count);
    }

    public async Task<IEnumerable<TranslationExport>> ExportTranslationsAsync(string languageCode, CancellationToken ct = default)
    {
        var config = await _configResolver.ResolveAsync(ct);
        if (string.IsNullOrWhiteSpace(config.ApiUrl) || string.IsNullOrWhiteSpace(config.ProjectId) || string.IsNullOrWhiteSpace(config.Component))
            return [];

        var request = CreateRequest(HttpMethod.Get, config,
            $"/api/translations/{config.ProjectId}/{config.Component}/{languageCode}/file/");
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Weblate export failed for {Language}: {StatusCode}", languageCode, response.StatusCode);
            return [];
        }

        try
        {
            using var stream = await response.Content.ReadAsStreamAsync(ct);
            var dict = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream, cancellationToken: ct);
            return dict?.Select(kvp => new TranslationExport(kvp.Key, kvp.Value)) ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Weblate export for {Language}", languageCode);
            return [];
        }
    }

    public async Task<IEnumerable<string>> GetAvailableLanguagesAsync(CancellationToken ct = default)
    {
        var config = await _configResolver.ResolveAsync(ct);
        if (string.IsNullOrWhiteSpace(config.ApiUrl) || string.IsNullOrWhiteSpace(config.ProjectId))
            return [];

        var request = CreateRequest(HttpMethod.Get, config, $"/api/projects/{config.ProjectId}/languages/");
        var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Weblate list languages failed: {StatusCode}", response.StatusCode);
            return [];
        }

        try
        {
            using var stream = await response.Content.ReadAsStreamAsync(ct);
            var langResponse = await JsonSerializer.DeserializeAsync<WeblateLanguagesResponse>(stream, cancellationToken: ct);
            return langResponse?.Results?.Select(l => l.Code) ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Weblate languages response");
            return [];
        }
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, TranslationConfiguration config, string path)
    {
        var request = new HttpRequestMessage(method, $"{config.ApiUrl!.TrimEnd('/')}{path}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        // Auth token is set on the HttpClient via DI configuration (SecretProvider)
        return request;
    }

    // Weblate API response models (internal)
    private sealed record WeblateLanguagesResponse(List<WeblateLanguage>? Results);
    private sealed record WeblateLanguage(string Code);
}

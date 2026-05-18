// ABOUTME: Weblate TMS provider implementation using Weblate REST API.
// ABOUTME: Supports import, export, language listing with Token authentication.

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Explore.Application.Contracts.Infrastructure;
using Microsoft.Extensions.Logging;
using Refit;

namespace Explore.Infrastructure.Localization;

/// <summary>
/// Weblate Translation Management System provider.
/// API docs: https://docs.weblate.org/en/latest/api.html
/// Auth: Authorization: Token {token} header.
/// </summary>
public class WeblateTranslationProvider : ITranslationManagementProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITranslationConfigResolver _configResolver;
    private readonly ILogger<WeblateTranslationProvider> _logger;

    public WeblateTranslationProvider(
        IHttpClientFactory httpClientFactory,
        ITranslationConfigResolver configResolver,
        ILogger<WeblateTranslationProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configResolver = configResolver;
        _logger = logger;
    }

    private IWeblateApi CreateApi(TranslationConfiguration config)
    {
        var client = _httpClientFactory.CreateClient("WeblateClient");
        client.BaseAddress = new Uri(config.ApiUrl!.TrimEnd('/'));
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return RestService.For<IWeblateApi>(client);
    }

    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        var config = await _configResolver.ResolveAsync(ct);
        if (string.IsNullOrWhiteSpace(config.ApiUrl) || string.IsNullOrWhiteSpace(config.ProjectId))
            return false;

        try
        {
            var api = CreateApi(config);
            var response = await api.TestConnectionAsync(config.ProjectId, ct);
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

                var payload = new WeblateUnitRequest
                {
                    Key = key.KeyName,
                    Value = new[] { translation }
                };

                var api = CreateApi(config);
                var response = await api.CreateTranslationUnitAsync(config.ProjectId, config.Component, lang, payload, ct);
                
                if (!response.IsSuccessStatusCode)
                {
                    var body = response.Error?.Content;
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

        var api = CreateApi(config);
        var response = await api.ExportTranslationsAsync(config.ProjectId, config.Component, languageCode, ct);
        
        if (!response.IsSuccessStatusCode || response.Content == null)
        {
            _logger.LogWarning("Weblate export failed for {Language}: {StatusCode}", languageCode, response.StatusCode);
            return [];
        }

        return response.Content.Select(kvp => new TranslationExport(kvp.Key, kvp.Value));
    }

    public async Task<IEnumerable<string>> GetAvailableLanguagesAsync(CancellationToken ct = default)
    {
        var config = await _configResolver.ResolveAsync(ct);
        if (string.IsNullOrWhiteSpace(config.ApiUrl) || string.IsNullOrWhiteSpace(config.ProjectId))
            return [];

        var api = CreateApi(config);
        var response = await api.GetLanguagesAsync(config.ProjectId, ct);

        if (!response.IsSuccessStatusCode || response.Content == null)
        {
            _logger.LogWarning("Weblate list languages failed: {StatusCode}", response.StatusCode);
            return [];
        }

        return response.Content?.Results?.Select(l => l.Code) ?? [];
    }

    // Weblate API response models (internal)
    internal sealed record WeblateLanguagesResponse(
        [property: JsonPropertyName("results")] List<WeblateLanguage>? Results);
    internal sealed record WeblateLanguage(
        [property: JsonPropertyName("code")] string Code);
}

internal interface IWeblateApi
{
    [Get("/api/projects/{projectId}/")]
    Task<IApiResponse> TestConnectionAsync(string projectId, CancellationToken cancellationToken = default);

    [Post("/api/translations/{projectId}/{component}/{languageCode}/units/")]
    Task<IApiResponse> CreateTranslationUnitAsync(string projectId, string component, string languageCode, [Body] WeblateUnitRequest request, CancellationToken cancellationToken = default);

    [Get("/api/translations/{projectId}/{component}/{languageCode}/file/")]
    Task<IApiResponse<Dictionary<string, string>>> ExportTranslationsAsync(string projectId, string component, string languageCode, CancellationToken cancellationToken = default);

    [Get("/api/projects/{projectId}/languages/")]
    Task<IApiResponse<WeblateTranslationProvider.WeblateLanguagesResponse>> GetLanguagesAsync(string projectId, CancellationToken cancellationToken = default);
}

internal class WeblateUnitRequest
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string[] Value { get; set; } = Array.Empty<string>();
}

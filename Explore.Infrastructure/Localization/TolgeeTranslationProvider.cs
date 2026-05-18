// ABOUTME: Tolgee TMS provider implementation using Tolgee REST API v2.
// ABOUTME: Supports import, export, language listing with X-API-Key authentication.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Explore.Application.Contracts.Infrastructure;
using Microsoft.Extensions.Logging;
using Refit;

namespace Explore.Infrastructure.Localization;

/// <summary>
/// Tolgee Translation Management System provider.
/// API docs: https://tolgee.io/api
/// Auth: X-API-Key header.
/// </summary>
public class TolgeeTranslationProvider : ITranslationManagementProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITranslationConfigResolver _configResolver;
    private readonly ILogger<TolgeeTranslationProvider> _logger;

    public TolgeeTranslationProvider(
        IHttpClientFactory httpClientFactory,
        ITranslationConfigResolver configResolver,
        ILogger<TolgeeTranslationProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configResolver = configResolver;
        _logger = logger;
    }

    private ITolgeeApi CreateApi(TranslationConfiguration config)
    {
        var client = _httpClientFactory.CreateClient("TolgeeClient");
        client.BaseAddress = new Uri(config.ApiUrl!.TrimEnd('/'));
        // Maintain the application/json accept header requirement
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return RestService.For<ITolgeeApi>(client);
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

        var payload = new TolgeeImportRequest
        {
            Keys = keyList.Select(k => new TolgeeImportKey
            {
                Name = k.KeyName,
                Translations = k.Translations
            }).ToList()
        };

        var api = CreateApi(config);
        var response = await api.ImportKeysAsync(config.ProjectId, payload, ct);
        
        if (!response.IsSuccessStatusCode)
        {
            var body = response.Error?.Content;
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

        var api = CreateApi(config);
        var response = await api.ExportTranslationsAsync(config.ProjectId, languageCode, ct);
        
        if (!response.IsSuccessStatusCode || response.Content == null)
        {
            _logger.LogWarning("Tolgee export failed for {Language}: {StatusCode}", languageCode, response.StatusCode);
            return [];
        }

        var tolgeeResponse = response.Content;
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

    public async Task<IEnumerable<string>> GetAvailableLanguagesAsync(CancellationToken ct = default)
    {
        var config = await _configResolver.ResolveAsync(ct);
        if (string.IsNullOrWhiteSpace(config.ApiUrl) || string.IsNullOrWhiteSpace(config.ProjectId))
            return [];

        var api = CreateApi(config);
        var response = await api.GetLanguagesAsync(config.ProjectId, ct);

        if (!response.IsSuccessStatusCode || response.Content == null)
        {
            _logger.LogWarning("Tolgee list languages failed: {StatusCode}", response.StatusCode);
            return [];
        }

        return response.Content?._embedded?.Languages?.Select(l => l.Tag) ?? [];
    }

    // Tolgee API response models (internal)
    internal sealed record TolgeeTranslationsResponse(
        [property: JsonPropertyName("_embedded")] TolgeeTranslationsEmbedded? _embedded);
    internal sealed record TolgeeTranslationsEmbedded(List<TolgeeKeyWithTranslation>? Keys);
    internal sealed record TolgeeKeyWithTranslation(
        [property: JsonPropertyName("name")] string KeyName, 
        Dictionary<string, TolgeeTranslationValue> Translations);
    internal sealed record TolgeeTranslationValue(string? Text);
    internal sealed record TolgeeLanguagesResponse(
        [property: JsonPropertyName("_embedded")] TolgeeLanguagesEmbedded? _embedded);
    internal sealed record TolgeeLanguagesEmbedded(List<TolgeeLanguage>? Languages);
    internal sealed record TolgeeLanguage(string Tag);
}

internal interface ITolgeeApi
{
    [Get("/v2/projects/{projectId}")]
    Task<IApiResponse> TestConnectionAsync(string projectId, CancellationToken cancellationToken = default);

    [Post("/v2/projects/{projectId}/keys/import-resolvable")]
    Task<IApiResponse> ImportKeysAsync(string projectId, [Body] TolgeeImportRequest request, CancellationToken cancellationToken = default);

    [Get("/v2/projects/{projectId}/translations/{languageCode}?structureDelimiter=.")]
    Task<IApiResponse<TolgeeTranslationProvider.TolgeeTranslationsResponse>> ExportTranslationsAsync(string projectId, string languageCode, CancellationToken cancellationToken = default);

    [Get("/v2/projects/{projectId}/languages")]
    Task<IApiResponse<TolgeeTranslationProvider.TolgeeLanguagesResponse>> GetLanguagesAsync(string projectId, CancellationToken cancellationToken = default);
}

internal class TolgeeImportRequest
{
    [JsonPropertyName("keys")]
    public List<TolgeeImportKey> Keys { get; set; } = new();
}

internal class TolgeeImportKey
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("translations")]
    public IDictionary<string, string> Translations { get; set; } = new Dictionary<string, string>();
}


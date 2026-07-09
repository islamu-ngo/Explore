// ABOUTME: HTTP service for the localization admin endpoints — test connection, export, governance update.
// ABOUTME: Refit-based typed API client registered via server-side AddTypedApiRefitClient.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services;
using Refit;

namespace Explore.Blazor.Client.Services;

public sealed class LocalizationAdminService : ILocalizationAdminService
{
    private readonly ILocalizationAdminApi _api;
    private readonly ILogger<LocalizationAdminService> _logger;

    public LocalizationAdminService(
        ILocalizationAdminApi api,
        ILogger<LocalizationAdminService> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<LocalizationConfigDto?> GetConfigurationAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _api.GetConfigurationAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[LOCALIZATION ADMIN] GET configuration returned {Status}", (int)response.StatusCode);
                return null;
            }
            return response.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LOCALIZATION ADMIN] Failed to fetch configuration");
            return null;
        }
    }

    public async Task<LocalizationAdminCommandResult> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _api.TestConnectionAsync(ct);
            return MapCommandResult(response, "TMS connection OK.", "TMS connection failed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LOCALIZATION ADMIN] Test connection failed");
            return new LocalizationAdminCommandResult(false, "Test connection failed: " + ex.Message);
        }
    }

    public async Task<LocalizationAdminCommandResult> RotateTmsApiKeyAsync(string apiKey, CancellationToken ct = default)
    {
        try
        {
            var response = await _api.RotateTmsApiKeyAsync(new RotateLocalizationTmsApiKeyDto
            {
                TmsApiKey = apiKey
            }, ct);

            return MapCommandResult(response, "TMS API key updated.", "TMS API key update failed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LOCALIZATION ADMIN] TMS API key rotation failed");
            return new LocalizationAdminCommandResult(false, "TMS API key update failed: " + ex.Message);
        }
    }

    public async Task<LocalizationAdminCommandResult> ExportFromTmsAsync(string languageCode, CancellationToken ct = default)
    {
        try
        {
            var response = await _api.ExportFromTmsAsync(languageCode, ct);
            return MapCommandResult(response, $"Exported translations for '{languageCode}'.", $"Export failed for '{languageCode}'.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LOCALIZATION ADMIN] Export failed for {Language}", languageCode);
            return new LocalizationAdminCommandResult(false, "Export failed: " + ex.Message);
        }
    }

    public async Task<IReadOnlyDictionary<string, string>?> ExportBundleAsync(string languageCode, CancellationToken ct = default)
    {
        try
        {
            var response = await _api.ExportBundleAsync(languageCode, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[LOCALIZATION ADMIN] Static bundle export returned {Status} for {Language}", (int)response.StatusCode, languageCode);
                return null;
            }

            return response.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LOCALIZATION ADMIN] Static bundle export failed for {Language}", languageCode);
            return null;
        }
    }

    public async Task<LocalizationAdminCommandResult> ImportBundleAsync(
        string languageCode,
        IReadOnlyDictionary<string, string> translations,
        CancellationToken ct = default)
    {
        try
        {
            var request = new ImportLocalizationBundleDto
            {
                LanguageCode = languageCode,
                Translations = translations.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            };

            var response = await _api.ImportBundleAsync(request, ct);
            return MapCommandResult(response, $"Imported static bundle for '{languageCode}'.", $"Static bundle import failed for '{languageCode}'.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LOCALIZATION ADMIN] Static bundle import failed for {Language}", languageCode);
            return new LocalizationAdminCommandResult(false, "Static bundle import failed: " + ex.Message);
        }
    }

    public async Task<LocalizationAdminCommandResult> UpdateGovernanceAsync(UpdateLocalizationGovernanceDto payload, CancellationToken ct = default)
    {
        try
        {
            var response = await _api.UpdateGovernanceAsync(payload, ct);
            return MapCommandResult(response, "Localization governance saved.", "Localization governance save failed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LOCALIZATION ADMIN] Governance update failed");
            return new LocalizationAdminCommandResult(false, "Governance update failed: " + ex.Message);
        }
    }

    public async Task<BundlePathHealthResult?> GetBundlePathHealthAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _api.GetBundlePathHealthAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[LOCALIZATION ADMIN] GET bundle-health returned {Status}", (int)response.StatusCode);
                return null;
            }
            return response.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LOCALIZATION ADMIN] Failed to fetch bundle path health");
            return null;
        }
    }

    private LocalizationAdminCommandResult MapCommandResult(
        IApiResponse<LocalizationAdminCommandResponse> response,
        string successFallback,
        string failureFallback)
    {
        if (!response.IsSuccessStatusCode)
        {
            return new LocalizationAdminCommandResult(false, failureFallback);
        }

        var content = response.Content;
        if (content is not null)
        {
            return new LocalizationAdminCommandResult(
                content.Success,
                string.IsNullOrWhiteSpace(content.Message)
                    ? successFallback
                    : content.Message);
        }

        return new LocalizationAdminCommandResult(true, successFallback);
    }
}

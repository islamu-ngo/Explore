// ABOUTME: HTTP service for the localization admin endpoints — test connection, export, governance update.
// ABOUTME: Typed HttpClient registered via AddTypedApiClient (matches FooterAdminService pattern).

using System.Net.Http.Json;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services;
using Explore.Blazor.Client.Models.Admin;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

public sealed class LocalizationAdminService : ILocalizationAdminService
{
    private const string ConfigurationEndpoint = "/api/admin/localization/configuration";
    private const string TestConnectionEndpoint = "/api/admin/localization/test-connection";
    private const string ExportEndpoint = "/api/admin/localization/export-from-tms";
    private const string GovernanceEndpoint = "/api/admin/localization/governance";
    private const string BundleHealthEndpoint = "/api/admin/localization/bundle-health";

    private readonly HttpClient _httpClient;
    private readonly ILogger<LocalizationAdminService> _logger;

    public LocalizationAdminService(HttpClient httpClient, ILogger<LocalizationAdminService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<LocalizationConfigDto?> GetConfigurationAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(ConfigurationEndpoint, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[LOCALIZATION ADMIN] GET configuration returned {Status}", (int)response.StatusCode);
                return null;
            }
            return await response.Content.ReadFromJsonAsync<LocalizationConfigDto>(ct);
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
            var response = await _httpClient.PostAsync(TestConnectionEndpoint, content: null, ct);
            return await BuildResultAsync(response, successFallback: "TMS connection OK.", failureFallback: "TMS connection failed.", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LOCALIZATION ADMIN] Test connection failed");
            return new LocalizationAdminCommandResult(false, "Test connection failed: " + ex.Message);
        }
    }

    public async Task<LocalizationAdminCommandResult> ExportFromTmsAsync(string languageCode, CancellationToken ct = default)
    {
        try
        {
            var url = $"{ExportEndpoint}?languageCode={Uri.EscapeDataString(languageCode)}";
            var response = await _httpClient.PostAsync(url, content: null, ct);
            return await BuildResultAsync(
                response,
                successFallback: $"Exported translations for '{languageCode}'.",
                failureFallback: $"Export failed for '{languageCode}'.",
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LOCALIZATION ADMIN] Export failed for {Language}", languageCode);
            return new LocalizationAdminCommandResult(false, "Export failed: " + ex.Message);
        }
    }

    public async Task<LocalizationAdminCommandResult> UpdateGovernanceAsync(LocalizationGovernancePayload payload, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(GovernanceEndpoint, payload, ct);
            return await BuildResultAsync(
                response,
                successFallback: "Localization governance saved.",
                failureFallback: "Localization governance save failed.",
                ct);
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
            var response = await _httpClient.GetAsync(BundleHealthEndpoint, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[LOCALIZATION ADMIN] GET bundle-health returned {Status}", (int)response.StatusCode);
                return null;
            }
            return await response.Content.ReadFromJsonAsync<BundlePathHealthResult>(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LOCALIZATION ADMIN] Failed to fetch bundle path health");
            return null;
        }
    }

    private async Task<LocalizationAdminCommandResult> BuildResultAsync(
        HttpResponseMessage response,
        string successFallback,
        string failureFallback,
        CancellationToken ct)
    {
        try
        {
            var raw = await response.Content.ReadAsStringAsync(ct);
            if (!string.IsNullOrWhiteSpace(raw))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(raw);
                var root = doc.RootElement;
                var success = response.IsSuccessStatusCode;
                string? message = null;
                if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    if (root.TryGetProperty("success", out var sEl) && sEl.ValueKind is System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False)
                    {
                        success = success && sEl.GetBoolean();
                    }
                    if (root.TryGetProperty("message", out var mEl) && mEl.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        message = mEl.GetString();
                    }
                }
                return new LocalizationAdminCommandResult(
                    success,
                    string.IsNullOrWhiteSpace(message)
                        ? (response.IsSuccessStatusCode ? successFallback : failureFallback)
                        : message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[LOCALIZATION ADMIN] Response body parse failed");
        }

        return new LocalizationAdminCommandResult(
            response.IsSuccessStatusCode,
            response.IsSuccessStatusCode ? successFallback : failureFallback);
    }
}

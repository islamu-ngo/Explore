// ABOUTME: Refit interface for localization admin API endpoints.
// ABOUTME: Covers config read, test connection, export, governance update, and bundle health.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services;
using Refit;

namespace Explore.Blazor.Client.Services;

public interface ILocalizationAdminApi
{
    [Get("/api/admin/localization/configuration")]
    Task<IApiResponse<LocalizationConfigDto>> GetConfigurationAsync(CancellationToken cancellationToken);

    [Post("/api/admin/localization/test-connection")]
    Task<IApiResponse<LocalizationAdminCommandResponse>> TestConnectionAsync(CancellationToken cancellationToken);

    [Post("/api/admin/localization/tms-api-key/rotate")]
    Task<IApiResponse<LocalizationAdminCommandResponse>> RotateTmsApiKeyAsync([Body] RotateLocalizationTmsApiKeyDto request, CancellationToken cancellationToken);

    [Post("/api/admin/localization/export-from-tms")]
    Task<IApiResponse<LocalizationAdminCommandResponse>> ExportFromTmsAsync([AliasAs("languageCode")] string languageCode, CancellationToken cancellationToken);

    [Get("/api/admin/localization/bundle")]
    Task<IApiResponse<Dictionary<string, string>>> ExportBundleAsync([AliasAs("languageCode")] string languageCode, CancellationToken cancellationToken);

    [Post("/api/admin/localization/bundle")]
    Task<IApiResponse<LocalizationAdminCommandResponse>> ImportBundleAsync([Body] ImportLocalizationBundleDto request, CancellationToken cancellationToken);

    [Put("/api/admin/localization/governance")]
    Task<IApiResponse<LocalizationAdminCommandResponse>> UpdateGovernanceAsync([Body] UpdateLocalizationGovernanceDto payload, CancellationToken cancellationToken);

    [Get("/api/admin/localization/bundle-health")]
    Task<IApiResponse<BundlePathHealthResult>> GetBundlePathHealthAsync(CancellationToken cancellationToken);
}

public class LocalizationAdminCommandResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

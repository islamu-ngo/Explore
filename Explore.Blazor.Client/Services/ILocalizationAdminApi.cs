// ABOUTME: Refit interface for localization admin API endpoints.
// ABOUTME: Covers config read, test connection, export, governance update, and bundle health.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services;
using Explore.Blazor.Client.Models.Admin;
using Refit;

namespace Explore.Blazor.Client.Services;

public interface ILocalizationAdminApi
{
    [Get("/api/admin/localization/configuration")]
    Task<IApiResponse<LocalizationConfigDto>> GetConfigurationAsync(CancellationToken cancellationToken);

    [Post("/api/admin/localization/test-connection")]
    Task<IApiResponse<LocalizationAdminCommandResponse>> TestConnectionAsync(CancellationToken cancellationToken);

    [Post("/api/admin/localization/export-from-tms")]
    Task<IApiResponse<LocalizationAdminCommandResponse>> ExportFromTmsAsync([AliasAs("languageCode")] string languageCode, CancellationToken cancellationToken);

    [Put("/api/admin/localization/governance")]
    Task<IApiResponse<LocalizationAdminCommandResponse>> UpdateGovernanceAsync([Body] LocalizationGovernancePayload payload, CancellationToken cancellationToken);

    [Get("/api/admin/localization/bundle-health")]
    Task<IApiResponse<BundlePathHealthResult>> GetBundlePathHealthAsync(CancellationToken cancellationToken);
}

public class LocalizationAdminCommandResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

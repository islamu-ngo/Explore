// ABOUTME: Defines the client boundary for HAL-gated whole-instance configuration-manifest downloads.
// ABOUTME: Keeps the browser on a fixed same-origin BFF route and exposes no bearer token or raw API URL.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.ControlPlane;

public interface IConfigurationManifestExportService
{
    Task<HalResourceOfControlPlaneOverviewDto> GetCapabilitiesAsync(
        CancellationToken cancellationToken = default);

    Task<ConfigurationManifestDownloadResult> DownloadAsync(
        ConfigurationManifestExportView view,
        CancellationToken cancellationToken = default);
}

// ABOUTME: Defines control-plane overview access for shared Blazor components.
// ABOUTME: Returns the generated API HAL resource without a local transport-model mirror.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.ControlPlane;

public interface IControlPlaneOverviewService
{
    Task<HalResourceOfControlPlaneOverviewDto> GetOverviewAsync(
        CancellationToken cancellationToken = default);
}

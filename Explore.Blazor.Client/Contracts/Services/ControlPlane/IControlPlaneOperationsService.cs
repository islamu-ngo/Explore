// ABOUTME: Defines control-plane operations access for shared Blazor pages.
// ABOUTME: Uses generated API resources and command responses end to end.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.ControlPlane;

public interface IControlPlaneOperationsService
{
    Task<HalResourceOfControlPlaneOperationsDto> GetOperationsAsync(CancellationToken cancellationToken = default);

    Task<HalResourceOfControlPlaneDeploymentModeRunbookDto> GetDeploymentModeRunbookAsync(
        CancellationToken cancellationToken = default);

    Task<BaseCommandResponseOfControlPlaneDeploymentModeTransitionDto> TransitionDeploymentModeAsync(
        string targetMode,
        string confirmationText,
        string? reason = null,
        CancellationToken cancellationToken = default);
}

// ABOUTME: Defines the host-provided operations read adapter contract for shared control-plane pages.
// ABOUTME: Lets embedded and separate hosts supply live operations data without adding transport details to the RCL.

using Event.ControlPlane.Client.Contracts;

namespace Event.ControlPlane.Client.Services;

public interface IControlPlaneOperationsService
{
    Task<ControlPlaneResult<ControlPlaneOperations>> GetOperationsAsync(CancellationToken cancellationToken = default);

    Task<ControlPlaneResult<ControlPlaneDeploymentModeRunbook>> GetDeploymentModeRunbookAsync(
        CancellationToken cancellationToken = default);

    Task<ControlPlaneCommandResult> TransitionDeploymentModeAsync(
        string targetMode,
        string confirmationText,
        string? reason = null,
        CancellationToken cancellationToken = default);
}

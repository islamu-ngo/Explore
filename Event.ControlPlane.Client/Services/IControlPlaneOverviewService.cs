// ABOUTME: Defines the host-provided overview service contract for shared control-plane components.
// ABOUTME: Keeps overview pages independent from generated clients, BFF hosts, and transport exceptions.

using Event.ControlPlane.Client.Contracts;

namespace Event.ControlPlane.Client.Services;

public interface IControlPlaneOverviewService
{
    Task<ControlPlaneResult<ControlPlaneOverview>> GetOverviewAsync(
        CancellationToken cancellationToken = default);
}

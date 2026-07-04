// ABOUTME: Defines the host-provided tenant service contract for shared control-plane components.
// ABOUTME: Keeps tenant lifecycle UI independent from generated clients, BFF hosts, and local authorization checks.

using Event.ControlPlane.Client.Contracts;

namespace Event.ControlPlane.Client.Services;

public interface IControlPlaneTenantService
{
    Task<ControlPlaneResult<ControlPlaneTenantList>> GetTenantsAsync(
        CancellationToken cancellationToken = default);
}

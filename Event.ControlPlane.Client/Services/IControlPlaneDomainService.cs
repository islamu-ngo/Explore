// ABOUTME: Defines the host-provided domain/DNS service contract for shared control-plane components.
// ABOUTME: Keeps domain pages independent from generated clients, BFF hosts, and transport exceptions.

using Event.ControlPlane.Client.Contracts;

namespace Event.ControlPlane.Client.Services;

public interface IControlPlaneDomainService
{
    Task<ControlPlaneResult<ControlPlaneDomainList>> GetDomainsAsync(
        CancellationToken cancellationToken = default);
}

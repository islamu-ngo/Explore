// ABOUTME: Defines the client read seam for the machine ticketing deployment capability matrix.
// ABOUTME: Keeps status display separate from generated transport details and exposes no mutation method.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services;

public interface ITicketingDeploymentCapabilityService
{
    Task<TicketingDeploymentCapabilityMatrixDto> GetAsync(
        CancellationToken cancellationToken);
}

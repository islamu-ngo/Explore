// ABOUTME: Reads ticketing deployment capability status through the generated Event API client.
// ABOUTME: Preserves the read-only contract and never derives production approval in the browser.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services;

namespace Explore.Blazor.Client.Services;

public sealed class TicketingDeploymentCapabilityService(
    ITicketingDeploymentCapabilitiesClient apiClient) :
    ITicketingDeploymentCapabilityService
{
    public Task<TicketingDeploymentCapabilityMatrixDto> GetAsync(
        CancellationToken cancellationToken) =>
        apiClient.GetTicketingDeploymentCapabilitiesAsync(
            cancellationToken: cancellationToken);
}

// ABOUTME: Defines the outbound Event-to-Control-Plane registration callback boundary.
// ABOUTME: Keeps the public Event Application layer independent from the private Control Plane implementation.

using Explore.Application.DTOs.Management;

namespace Explore.Application.Contracts.Infrastructure;

public interface IManagedControlPlaneRegistrationClient
{
    Task<CompleteManagedInstanceRegistrationResponse> CompleteRegistrationAsync(
        Uri controlPlaneUrl,
        CompleteManagedInstanceRegistrationRequest request,
        CancellationToken cancellationToken);
}

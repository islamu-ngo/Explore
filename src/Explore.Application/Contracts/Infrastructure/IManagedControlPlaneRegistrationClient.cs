// ABOUTME: Defines the outbound Event-to-Control-Plane registration callback boundary.
// ABOUTME: Keeps the public Event Application layer independent from the private Control Plane implementation.

using Explore.Application.DTOs.Management;

namespace Explore.Application.Contracts.Infrastructure;

public interface IManagedControlPlaneRegistrationClient
{
    Task<CompleteManagedInstanceRegistrationResponseDto> CompleteRegistrationAsync(
        Uri controlPlaneUrl,
        CompleteManagedInstanceRegistrationRequestDto request,
        CancellationToken cancellationToken);
}

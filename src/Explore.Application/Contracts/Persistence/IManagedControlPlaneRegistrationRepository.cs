// ABOUTME: Defines entity-returning persistence operations for the optional managed Control Plane registration.
// ABOUTME: Supports singleton lifecycle reads and dedicated inbound machine-credential authentication lookup.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IManagedControlPlaneRegistrationRepository
    : IGenericRepository<ManagedControlPlaneRegistration, Guid>
{
    Task<ManagedControlPlaneRegistration?> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task<ManagedControlPlaneRegistration?> GetActiveByControlPlaneKeyIdAsync(
        string keyId,
        CancellationToken cancellationToken = default);
}

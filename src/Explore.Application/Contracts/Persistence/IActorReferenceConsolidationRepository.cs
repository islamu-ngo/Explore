// ABOUTME: Defines the atomic persistence operation for moving mutable Actor operational references.
// ABOUTME: Keeps collision detection in Persistence while immutable evidence stays on the source.

namespace Explore.Application.Contracts.Persistence;

public interface IActorReferenceConsolidationRepository
{
    Task<bool> MoveMutableReferencesAsync(Guid sourceActorId, Guid canonicalActorId, int canonicalActorTypeId, CancellationToken cancellationToken = default);
    Task<bool> HasCompletedConsolidationAsync(Guid atprotoIdentityId, Guid canonicalActorId, string evidenceReference, CancellationToken cancellationToken = default);
}

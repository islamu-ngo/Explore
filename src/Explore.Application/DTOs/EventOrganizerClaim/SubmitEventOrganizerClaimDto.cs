// ABOUTME: Input contract for a claimant actor requesting organizer authority over an event.
// ABOUTME: Carries bounded evidence references while actor ownership is verified server-side.

namespace Explore.Application.DTOs.EventOrganizerClaim;

public sealed record SubmitEventOrganizerClaimDto
{
    public Guid ClaimantActorId { get; init; }
    public required string EvidenceType { get; init; }
    public required string EvidenceReference { get; init; }
}

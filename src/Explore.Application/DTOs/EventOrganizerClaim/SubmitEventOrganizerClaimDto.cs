// ABOUTME: Input contract for a claimant actor requesting organizer authority over an event.
// ABOUTME: Carries bounded evidence references while actor ownership is verified server-side.

namespace Explore.Application.DTOs.EventOrganizerClaim;

public sealed class SubmitEventOrganizerClaimDto
{
    public Guid ClaimantActorId { get; set; }
    public required string EvidenceType { get; set; }
    public required string EvidenceReference { get; set; }
}

// ABOUTME: Curator input for one explicit organizer-claim review transition.
// ABOUTME: Includes optimistic concurrency and a stable reason code for auditable decisions.

namespace Explore.Application.DTOs.EventOrganizerClaim;

public sealed record ReviewEventOrganizerClaimDto
{
    public EventOrganizerClaimReviewDecisionDto Decision { get; init; }
    public required string ReasonCode { get; init; }
    public Guid ExpectedConcurrencyStamp { get; init; }
}

public enum EventOrganizerClaimReviewDecisionDto
{
    RequestEvidence = 1,
    Approve = 2,
    Reject = 3
}

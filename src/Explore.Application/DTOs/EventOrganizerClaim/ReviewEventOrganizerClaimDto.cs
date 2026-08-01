// ABOUTME: Curator input for one explicit organizer-claim review transition.
// ABOUTME: Includes optimistic concurrency and a stable reason code for auditable decisions.

namespace Explore.Application.DTOs.EventOrganizerClaim;

public sealed class ReviewEventOrganizerClaimDto
{
    public EventOrganizerClaimReviewDecisionDto Decision { get; set; }
    public required string ReasonCode { get; set; }
    public Guid ExpectedConcurrencyStamp { get; set; }
}

public enum EventOrganizerClaimReviewDecisionDto
{
    RequestEvidence = 1,
    Approve = 2,
    Reject = 3
}

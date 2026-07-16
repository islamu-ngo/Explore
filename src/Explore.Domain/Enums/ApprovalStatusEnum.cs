// ABOUTME: Stable persisted identifiers for approval and registration lifecycle states.
// ABOUTME: Cancelled and Revoked are terminal states that must never grant live attendee authority.

namespace Explore.Domain.Enums;

public enum ApprovalStatusEnum
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Waitlisted = 4,
    Cancelled = 5,
    Revoked = 6
}

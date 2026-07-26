// ABOUTME: Canonical integer identifiers for organizer claim lifecycle states.
// ABOUTME: Values must remain aligned with persistence seeding and API lookup metadata.

namespace Explore.Domain.Enums;

public enum EventOrganizerClaimStatusEnum
{
    Pending = 1,
    EvidenceRequired = 2,
    Approved = 3,
    Rejected = 4,
    Withdrawn = 5,
    Expired = 6
}

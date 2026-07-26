// ABOUTME: Canonical integer identifiers for public event action health states.
// ABOUTME: Values must remain aligned with persistence seeding and API lookup metadata.

namespace Explore.Domain.Enums;

public enum EventPublicActionHealthStateEnum
{
    PendingReview = 1,
    Active = 2,
    Broken = 3,
    Unsafe = 4,
    Disabled = 5,
    Expired = 6
}

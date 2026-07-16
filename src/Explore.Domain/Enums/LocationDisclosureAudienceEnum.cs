// ABOUTME: Stable integer identifiers for event-location disclosure audiences.
// ABOUTME: Audience policy remains independent from physical location classification.

namespace Explore.Domain.Enums;

public enum LocationDisclosureAudienceEnum
{
    Never = 1,
    AnyCurrentRegistrant = 2,
    ConfirmedParticipant = 3
}

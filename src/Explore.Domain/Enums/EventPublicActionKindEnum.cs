// ABOUTME: Canonical integer identifiers for public event action kinds.
// ABOUTME: Values must remain aligned with persistence seeding and API lookup metadata.

namespace Explore.Domain.Enums;

public enum EventPublicActionKindEnum
{
    OriginalSource = 1,
    ExternalEventPage = 2,
    ExternalRegistration = 3,
    OptionalQuestionnaire = 4,
    Livestream = 5,
    OrganizerContact = 6
}

// ABOUTME: Canonical integer identifiers for participation handling mode lookup rows.
// ABOUTME: Values are stable contracts and must remain aligned with future persistence seeding.

namespace Explore.Domain.Enums;

public enum ParticipationHandlingModeEnum
{
    InformationOnly = 1,
    WalkIn = 2,
    ExternalManaged = 3,
    PlatformManaged = 4
}

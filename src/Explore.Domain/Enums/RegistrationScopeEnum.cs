// ABOUTME: Enum mirror of the RegistrationScope lookup expressing why a user registered (whole event, whole day, or picked sessions).
// ABOUTME: Values are stable lookup ids consumed by EventRegistrationIntent and the policy-aware registration UX.

namespace Explore.Domain.Enums;

public enum RegistrationScopeEnum
{
    Event = 1,
    Day = 2,
    SessionSelection = 3
}

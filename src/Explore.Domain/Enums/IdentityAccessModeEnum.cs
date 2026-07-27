// ABOUTME: Canonical integer identifiers for identity-access mode lookup rows.
// ABOUTME: Values are stable contracts and must remain aligned with future persistence seeding.

namespace Explore.Domain.Enums;

public enum IdentityAccessModeEnum
{
    AccountRequired = 1,
    GuestAllowed = 2,
    CapabilityTokenAllowed = 3
}

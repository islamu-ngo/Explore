// ABOUTME: Stable integer identifiers for physical location privacy lifecycle states.
// ABOUTME: Distinguishes absent PII, active PII, and irreversibly erased PII.

namespace Explore.Domain.Enums;

public enum LocationPrivacyStateEnum
{
    NotProvided = 1,
    Active = 2,
    Erased = 3
}

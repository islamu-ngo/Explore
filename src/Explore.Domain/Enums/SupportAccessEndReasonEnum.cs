// ABOUTME: Lookup enum for deterministic support-access terminal reasons.
// ABOUTME: Values support reporting without relying on free-form lifecycle text.

namespace Explore.Domain.Enums;

public enum SupportAccessEndReasonEnum
{
    UserStopped = 1,
    Expired = 2,
    ForceStopped = 3,
    RevokedByPolicy = 4,
    Replaced = 5
}

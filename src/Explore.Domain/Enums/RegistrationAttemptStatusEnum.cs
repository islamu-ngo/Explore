// ABOUTME: Enum mirror for stable registration-attempt runtime lifecycle lookup identities.
// ABOUTME: Keeps guest capability session transitions explicit in pure Domain code.

namespace Explore.Domain.Enums;

public enum RegistrationAttemptStatusEnum
{
    Active = 1,
    Consumed = 2,
    Expired = 3,
    Superseded = 4
}

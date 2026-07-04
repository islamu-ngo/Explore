// ABOUTME: Lookup enum for support-access session lifecycle states.
// ABOUTME: Values map to SupportAccessSessionStatus lookup rows and must remain stable.

namespace Explore.Domain.Enums;

public enum SupportAccessSessionStatusEnum
{
    PendingApproval = 1,
    Active = 2,
    Stopped = 3,
    Expired = 4,
    Revoked = 5
}

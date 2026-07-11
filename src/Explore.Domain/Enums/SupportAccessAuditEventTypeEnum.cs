// ABOUTME: Lookup enum for support-access audit event categories.
// ABOUTME: Values identify lifecycle, denial, request-observation, and command-commit evidence.

namespace Explore.Domain.Enums;

public enum SupportAccessAuditEventTypeEnum
{
    Started = 1,
    Stopped = 2,
    Expired = 3,
    Revoked = 4,
    Denied = 5,
    RequestObserved = 6,
    CommandCommitted = 7
}

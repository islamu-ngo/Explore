// ABOUTME: Stable provider-neutral status identities for durable refund attempts.
// ABOUTME: Distinguishes capacity-reserving ambiguity from definitively released terminals.

namespace Explore.Domain.Enums;

public enum RefundAttemptStatusEnum
{
    Requested = 1,
    DispatchPending = 2,
    Pending = 3,
    RequiresAction = 4,
    Unknown = 5,
    Succeeded = 6,
    Failed = 7,
    Cancelled = 8
}

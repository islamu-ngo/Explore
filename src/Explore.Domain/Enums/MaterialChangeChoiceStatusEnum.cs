// ABOUTME: Enumerates the durable buyer response to a paid-event material change.
// ABOUTME: Keeps pending, accepted, refund-requested, and uncaptured-terminal states explicit.

namespace Explore.Domain.Enums;

public enum MaterialChangeChoiceStatusEnum
{
    Pending = 0,
    AcceptedNewTerms = 1,
    RefundRequested = 2,
    NotApplicable = 3
}

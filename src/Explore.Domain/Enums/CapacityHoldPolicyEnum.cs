// ABOUTME: Enum mirror for stable capacity-hold policy lookup identities.
// ABOUTME: Defines when capacity is reserved or converted to a waitlist request.

namespace Explore.Domain.Enums;

public enum CapacityHoldPolicyEnum
{
    NoHoldUntilReady = 1,
    TimedHoldOnSelection = 2,
    ApprovalNoHold = 3,
    WaitlistWhenFull = 4
}

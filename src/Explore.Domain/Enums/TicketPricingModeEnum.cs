// ABOUTME: Enum mirror for the five stable TicketPricingMode lookup identities.
// ABOUTME: Keeps pricing rule branching exhaustive and independent from persistence navigation.

namespace Explore.Domain.Enums;

public enum TicketPricingModeEnum
{
    Fixed = 1,
    Free = 2,
    Donation = 3,
    PayWhatYouCan = 4,
    SlidingScale = 5
}

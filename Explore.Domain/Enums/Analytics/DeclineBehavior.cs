// ABOUTME: Defines what happens when a user declines cookie consent.
// ABOUTME: Cookieless = privacy-preserving analytics via server-side hash; Disable = no analytics at all.

namespace Explore.Domain.Enums.Analytics;

public enum DeclineBehavior
{
    Disable = 0,
    Cookieless = 1
}

// ABOUTME: Optional recommended actions attached to moderation signals.
// ABOUTME: Keeps automated recommendations advisory until a decision command executes them.

namespace Explore.Domain.Enums;

public enum EventReportRecommendedAction
{
    None = 0,
    Dismiss = 1,
    LightModerate = 2,
    HeavyRedact = 3,
    Escalate = 4
}

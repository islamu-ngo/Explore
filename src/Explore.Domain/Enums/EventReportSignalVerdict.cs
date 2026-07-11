// ABOUTME: Normalized verdicts produced by moderation signal providers.
// ABOUTME: Preserves provider recommendations without turning them into enforcement actions.

namespace Explore.Domain.Enums;

public enum EventReportSignalVerdict
{
    NoSignal = 1,
    NeedsReview = 2,
    LikelyViolation = 3,
    Urgent = 4,
    AutoActionRecommended = 5
}

// ABOUTME: Atlassian 4-question matrix outcome indicating whether a Layer 3 custom property should be promoted.
// ABOUTME: Computed by governance report queries; drives operator-visible promotion workflows.

namespace Explore.Domain.Enums;

public enum PromotionRecommendation
{
    None = 0,
    ConsiderProjectionFirst = 1,
    ConsiderLayer2Promotion = 2,
    ConsiderLayer1Promotion = 3
}

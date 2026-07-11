// ABOUTME: Filter parameters for the custom-property governance report query.
// ABOUTME: Supports scoping by entity type and promotion recommendation level.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.CustomPropertyGovernance;

public class GovernanceReportFilterDto
{
    public string? EntityScope { get; set; }
    public PromotionRecommendation? Recommendation { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

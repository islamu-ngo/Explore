// ABOUTME: Filter parameters for the custom-property governance report query.
// ABOUTME: Supports scoping by entity type and promotion recommendation level.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.CustomPropertyGovernance;

public sealed record GovernanceReportFilterDto
{
    public string? EntityScope { get; init; }
    public PromotionRecommendation? Recommendation { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

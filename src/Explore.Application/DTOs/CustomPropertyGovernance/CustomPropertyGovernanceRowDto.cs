// ABOUTME: Read-only DTO for the governance report listing all active Layer 3 definitions with promotion recommendations.
// ABOUTME: Implements Atlassian 4-question matrix (Rule 12) to surface candidates for Layer 2/Layer 1 promotion.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.CustomPropertyGovernance;

public sealed record CustomPropertyGovernanceRowDto
{
    public Guid TenantId { get; init; }
    public string Namespace { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string EntityScope { get; init; } = string.Empty;
    public string PropertyType { get; init; } = string.Empty;
    public ExposureLevel ExposureLevel { get; init; }
    public bool IsSearchable { get; init; }
    public bool IsFilterable { get; init; }
    public bool IsExportable { get; init; }
    public bool IsModerationRelevant { get; init; }
    public bool IsAnalyticsRelevant { get; init; }
    public bool IsSystemOwned { get; init; }
    public int ActiveInstanceCount { get; init; }
    public DateTime? LastUsedAt { get; init; }
    public PromotionRecommendation Recommendation { get; init; }
}

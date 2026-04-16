// ABOUTME: Read-only DTO for the governance report listing all active Layer 3 definitions with promotion recommendations.
// ABOUTME: Implements Atlassian 4-question matrix (Rule 12) to surface candidates for Layer 2/Layer 1 promotion.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.CustomPropertyGovernance;

public class CustomPropertyGovernanceRowDto
{
    public Guid TenantId { get; set; }
    public string Namespace { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string EntityScope { get; set; } = string.Empty;
    public string PropertyType { get; set; } = string.Empty;
    public ExposureLevel ExposureLevel { get; set; }
    public bool IsSearchable { get; set; }
    public bool IsFilterable { get; set; }
    public bool IsExportable { get; set; }
    public bool IsModerationRelevant { get; set; }
    public bool IsAnalyticsRelevant { get; set; }
    public bool IsSystemOwned { get; set; }
    public int ActiveInstanceCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public PromotionRecommendation Recommendation { get; set; }
}

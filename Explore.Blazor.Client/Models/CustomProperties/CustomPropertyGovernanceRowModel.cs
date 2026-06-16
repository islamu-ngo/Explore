// ABOUTME: Governance report row mirroring the server CustomPropertyGovernanceRowDto.
// ABOUTME: Carries active-instance counts and the Atlassian 4-question promotion recommendation.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Models.CustomProperties;

/// <summary>
/// One row of the governance report surfacing promotion recommendations for Layer 3 definitions.
/// </summary>
public sealed class CustomPropertyGovernanceRowModel
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
    public DateTimeOffset? LastUsedAt { get; set; }
    public PromotionRecommendation Recommendation { get; set; }
}

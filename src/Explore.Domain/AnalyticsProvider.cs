// ABOUTME: Lookup-table entity for analytics providers available to tenant and instance settings.
// ABOUTME: Source of truth for provider ids used by AnalyticsProviderEnum.

namespace Explore.Domain;

public class AnalyticsProvider
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}

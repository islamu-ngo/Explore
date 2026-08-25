// ABOUTME: Public read model for a tenant's coarse event-discovery area metadata.
// ABOUTME: Exposes no internal location IDs, venue coordinates, addresses, or other location PII.

namespace Explore.Application.DTOs.PublicExperience;

public sealed record PublicDiscoveryAreaDto
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public decimal? CentroidLatitude { get; init; }
    public decimal? CentroidLongitude { get; init; }
    public bool IsDefault { get; init; }
    public int SortOrder { get; init; }
}

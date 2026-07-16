// ABOUTME: Public read model for a tenant's coarse event-discovery area metadata.
// ABOUTME: Exposes no internal location IDs, venue coordinates, addresses, or other location PII.

namespace Explore.Application.DTOs.PublicExperience;

public sealed class PublicDiscoveryAreaDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public decimal? CentroidLatitude { get; set; }
    public decimal? CentroidLongitude { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
}

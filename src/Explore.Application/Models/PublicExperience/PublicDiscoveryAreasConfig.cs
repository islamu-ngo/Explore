// ABOUTME: Application-owned versioned configuration for tenant-governed public discovery areas.
// ABOUTME: Separates coarse public centroids from internal tenant location IDs and exact location PII.

namespace Explore.Application.Models.PublicExperience;

public sealed record PublicDiscoveryAreasConfig(
    int SchemaVersion = 1,
    IReadOnlyList<PublicDiscoveryAreaConfig>? Areas = null);

public sealed record PublicDiscoveryAreaConfig(
    Guid Id,
    string DisplayName,
    string City,
    string CountryCode,
    decimal? CentroidLatitude = null,
    decimal? CentroidLongitude = null,
    IReadOnlyList<Guid>? LocationIds = null,
    bool IsActive = true,
    bool IsDefault = false,
    int SortOrder = 0);

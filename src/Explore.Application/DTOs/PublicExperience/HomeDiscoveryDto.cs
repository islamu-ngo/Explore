// ABOUTME: Composite public-home discovery read model with bounded event sections and safe status metadata.
// ABOUTME: Reserves future proximity fields as null while exposing only coarse area context in this release.

using System.Text.Json.Serialization;
using Explore.Application.DTOs.Event;
using Explore.Application.Models.PublicExperience;

namespace Explore.Application.DTOs.PublicExperience;

public sealed record HomeDiscoveryDto
{
    public int SchemaVersion { get; init; } = 1;
    public HomeDiscoveryContextDto Context { get; set; } = new();
    public List<EventDiscoveryItemDto> Hero { get; set; } = [];
    public List<EventDiscoveryItemDto> UpcomingInArea { get; set; } = [];
    public HomeDiscoverySectionDto? Spotlight { get; set; }
    public List<EventDiscoveryItemDto> MostViewedInArea { get; set; } = [];
    public List<EventDiscoveryItemDto> MostViewedOnline { get; set; } = [];
    public List<HomeDiscoverySectionDto> CuratedSections { get; set; } = [];
    public List<EventDiscoveryItemDto> RecentlyAdded { get; set; } = [];
    public Dictionary<string, HomeDiscoverySectionStatus> SectionStatuses { get; init; } = [];
    public DateTimeOffset GeneratedAtUtc { get; init; }
}

public sealed record HomeDiscoveryContextDto
{
    public HomeDiscoveryMode Mode { get; init; } = HomeDiscoveryMode.All;
    public Guid? SelectedAreaId { get; init; }
    public Guid? DefaultAreaId { get; init; }
    public string SelectedAreaDisplayName { get; init; } = string.Empty;
    public List<PublicDiscoveryAreaDto> AvailableAreas { get; init; } = [];
}

public sealed record EventDiscoveryItemDto
{
    [JsonConstructor]
    public EventDiscoveryItemDto()
    {
    }

    public string Source { get; init; } = "local";
    public EventListDto? Event { get; init; }
    public FederatedEventDto? FederatedEvent { get; init; }
    public EventFederationMetadataDto? Federation { get; init; }
    public double? DistanceMeters { get; init; }
    public Guid? NearestSessionId { get; init; }
    public Guid? NearestLocationId { get; init; }
    public string? NearestLocationName { get; init; }
    public DateTimeOffset? NearestOccurrenceStartsAtUtc { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalProperties { get; set; } = [];
}

public sealed record FederatedEventDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset? StartsAtUtc { get; init; }
    public DateTimeOffset? EndsAtUtc { get; init; }
    public string? Mode { get; init; }
    public string? Status { get; init; }
    public bool? RsvpExpected { get; init; }
    public string? LocationSummary { get; init; }
}

public sealed record EventFederationMetadataDto
{
    public Guid AtprotoRecordId { get; init; }
    public string Provenance { get; init; } = string.Empty;
    public bool IsLocalEcho { get; init; }
    public bool HasSourceLink { get; set; }
}

public sealed record HomeDiscoverySectionDto
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public List<EventDiscoveryItemDto> Items { get; init; } = [];
}

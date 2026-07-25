// ABOUTME: Composite public-home discovery read model with bounded event sections and safe status metadata.
// ABOUTME: Reserves future proximity fields as null while exposing only coarse area context in this release.

using System.Text.Json.Serialization;
using Explore.Application.DTOs.Event;
using Explore.Application.Models.PublicExperience;

namespace Explore.Application.DTOs.PublicExperience;

public sealed class HomeDiscoveryDto
{
    public int SchemaVersion { get; set; } = 1;
    public HomeDiscoveryContextDto Context { get; set; } = new();
    public List<EventDiscoveryItemDto> Hero { get; set; } = [];
    public List<EventDiscoveryItemDto> UpcomingInArea { get; set; } = [];
    public HomeDiscoverySectionDto? Spotlight { get; set; }
    public List<EventDiscoveryItemDto> MostViewedInArea { get; set; } = [];
    public List<EventDiscoveryItemDto> MostViewedOnline { get; set; } = [];
    public List<HomeDiscoverySectionDto> CuratedSections { get; set; } = [];
    public List<EventDiscoveryItemDto> RecentlyAdded { get; set; } = [];
    public Dictionary<string, HomeDiscoverySectionStatus> SectionStatuses { get; set; } = [];
    public DateTimeOffset GeneratedAtUtc { get; set; }
}

public sealed class HomeDiscoveryContextDto
{
    public HomeDiscoveryMode Mode { get; set; } = HomeDiscoveryMode.All;
    public Guid? SelectedAreaId { get; set; }
    public Guid? DefaultAreaId { get; set; }
    public string SelectedAreaDisplayName { get; set; } = string.Empty;
    public List<PublicDiscoveryAreaDto> AvailableAreas { get; set; } = [];
}

public sealed class EventDiscoveryItemDto
{
    public string Source { get; set; } = "local";
    public EventListDto? Event { get; set; }
    public FederatedEventDto? FederatedEvent { get; set; }
    public EventFederationMetadataDto? Federation { get; set; }
    public double? DistanceMeters { get; set; }
    public Guid? NearestSessionId { get; set; }
    public Guid? NearestLocationId { get; set; }
    public string? NearestLocationName { get; set; }
    public DateTimeOffset? NearestOccurrenceStartsAtUtc { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalProperties { get; set; } = [];
}

public sealed class FederatedEventDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? StartsAtUtc { get; set; }
    public DateTimeOffset? EndsAtUtc { get; set; }
    public string? Mode { get; set; }
    public string? Status { get; set; }
    public bool? RsvpExpected { get; set; }
    public string? LocationSummary { get; set; }
}

public sealed class EventFederationMetadataDto
{
    public Guid AtprotoRecordId { get; set; }
    public string Provenance { get; set; } = string.Empty;
    public bool IsLocalEcho { get; set; }
    public bool HasSourceLink { get; set; }
}

public sealed class HomeDiscoverySectionDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public List<EventDiscoveryItemDto> Items { get; set; } = [];
}

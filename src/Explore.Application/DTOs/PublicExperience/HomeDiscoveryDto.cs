// ABOUTME: Composite public-home discovery read model with bounded event sections and safe status metadata.
// ABOUTME: Reserves future proximity fields as null while exposing only coarse area context in this release.

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
    public EventListDto Event { get; set; } = null!;
    public double? DistanceMeters { get; set; }
    public Guid? NearestSessionId { get; set; }
    public Guid? NearestLocationId { get; set; }
    public string? NearestLocationName { get; set; }
    public DateTimeOffset? NearestOccurrenceStartsAtUtc { get; set; }
}

public sealed class HomeDiscoverySectionDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public List<EventDiscoveryItemDto> Items { get; set; } = [];
}

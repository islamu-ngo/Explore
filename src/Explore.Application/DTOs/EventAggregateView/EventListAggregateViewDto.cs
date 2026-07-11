// ABOUTME: Shallow list-item DTO for the EventWithSessions aggregate read view.
// ABOUTME: Exposes key summary scalars plus a capped set of searchable public facets for discovery surfaces.

using System.Text.Json.Serialization;
using Explore.Application.Hateoas;

namespace Explore.Application.DTOs.EventAggregateView;

public sealed class EventListAggregateViewDto
{
    public Guid EventId { get; set; }
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset? EndAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Visibility { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? IslamicTheme { get; set; }
    public string? Madhab { get; set; }
    public bool? IsRamadan { get; set; }
    public bool? PrayerAware { get; set; }
    public string? TechStack { get; set; }
    public string? DifficultyLevel { get; set; }
    public string? TargetAudience { get; set; }
    public int SessionCount { get; set; }
    public DateTimeOffset? FirstSessionStartAt { get; set; }
    public DateTimeOffset? LastSessionEndAt { get; set; }
    public bool HasInPersonSessions { get; set; }
    public bool HasVirtualSessions { get; set; }
    public string? AggregatedSessionIslamicThemes { get; set; }
    public IReadOnlyList<EventCustomPropertyFacetDto> SearchableFacets { get; set; } = [];

    [JsonPropertyName("_links")]
    public Dictionary<string, HalLink> Links { get; set; } = new();
}

// ABOUTME: Detail DTO for the EventWithSessions aggregate read view consumed by app-layer queries.
// ABOUTME: Combines core event scalars, module-gated nullable aspect fields, summary metrics, and filtered facets.

using System.Text.Json.Serialization;
using Explore.Application.Hateoas;

namespace Explore.Application.DTOs.EventAggregateView;

public sealed class EventWithSessionsViewDto
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
    public IReadOnlyList<EventCustomPropertyFacetDto> EventCustomProperties { get; set; } = [];
    public IReadOnlyList<EventSessionCustomPropertyFacetDto> EventSessionCustomProperties { get; set; } = [];

    [JsonPropertyName("_links")]
    public Dictionary<string, HalLink> Links { get; set; } = new();
}

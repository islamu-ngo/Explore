// ABOUTME: Shallow list-item DTO for the EventWithSessions aggregate read view.
// ABOUTME: Exposes key summary scalars plus a capped set of searchable public facets for discovery surfaces.

using System.Text.Json.Serialization;
using Explore.Application.Hateoas;

namespace Explore.Application.DTOs.EventAggregateView;

public sealed record EventListAggregateViewDto
{
    public Guid EventId { get; init; }
    public Guid TenantId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTimeOffset StartAt { get; init; }
    public DateTimeOffset? EndAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Visibility { get; init; } = string.Empty;
    public bool IsDeleted { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public string? IslamicTheme { get; init; }
    public string? Madhab { get; init; }
    public bool? IsRamadan { get; init; }
    public bool? PrayerAware { get; init; }
    public string? TechStack { get; init; }
    public string? DifficultyLevel { get; init; }
    public string? TargetAudience { get; init; }
    public int SessionCount { get; init; }
    public DateTimeOffset? FirstSessionStartAt { get; init; }
    public DateTimeOffset? LastSessionEndAt { get; init; }
    public bool HasInPersonSessions { get; init; }
    public bool HasVirtualSessions { get; init; }
    public string? AggregatedSessionIslamicThemes { get; init; }
    public IReadOnlyList<EventCustomPropertyFacetDto> SearchableFacets { get; init; } = [];

    [JsonPropertyName("_links")]
    public Dictionary<string, HalLink> Links { get; init; } = new();
}

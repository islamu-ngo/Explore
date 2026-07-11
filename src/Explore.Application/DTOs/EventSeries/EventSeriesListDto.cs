// ABOUTME: Lightweight list DTO for event series, used in paginated list views.
// ABOUTME: Includes event count but not the full events collection.

using System;

namespace Explore.Application.DTOs.EventSeries;

public class EventSeriesListDto
{
    public Guid Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    public required string Title { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? FeaturedImageUri { get; set; }
    public Guid ActorId { get; set; }
    public string? ActorDisplayName { get; set; }
    public DateTimeOffset? StartDateUtc { get; set; }
    public DateTimeOffset? EndDateUtc { get; set; }
    public int EventCount { get; set; }
}

using System;

namespace Explore.Application.DTOs.Event;

public class EventSeriesListDto
{
    public Guid Id { get; set; }
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

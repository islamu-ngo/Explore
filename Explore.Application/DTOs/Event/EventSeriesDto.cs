using System;
using System.Collections.Generic;

namespace Explore.Application.DTOs.Event;

public class EventSeriesDto
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public Guid? FeaturedImageId { get; set; }
    public string? FeaturedImageUri { get; set; }
    public Guid ActorId { get; set; }
    public string? ActorDisplayName { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset? StartDateUtc { get; set; }
    public DateTimeOffset? EndDateUtc { get; set; }

    public List<EventListDto> Events { get; set; } = new();
}

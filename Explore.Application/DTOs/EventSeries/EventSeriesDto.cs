// ABOUTME: Full detail DTO for an event series, including its associated events list.
// ABOUTME: Returned by GetEventSeriesDetailRequest and GetTopEventSeriesRequest.

using System;
using System.Collections.Generic;
using Explore.Application.DTOs.Event;

namespace Explore.Application.DTOs.EventSeries;

public class EventSeriesDto
{
    public Guid Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
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

// ABOUTME: DTO for creating a new event series.
// ABOUTME: Carries title, description, slug, featured image, actor, and publish state.

using System;

namespace Explore.Application.DTOs.EventSeries;

public class CreateEventSeriesDto
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? Slug { get; set; }
    public Guid? FeaturedImageId { get; set; }
    public Guid ActorId { get; set; }
    public bool IsPublished { get; set; }
}

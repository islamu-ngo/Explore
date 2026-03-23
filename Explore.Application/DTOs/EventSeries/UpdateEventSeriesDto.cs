// ABOUTME: DTO for updating an existing event series.
// ABOUTME: Carries the target series ID plus all mutable fields (title, description, slug, image, publish state).

using System;

namespace Explore.Application.DTOs.EventSeries;

public class UpdateEventSeriesDto
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? Slug { get; set; }
    public Guid? FeaturedImageId { get; set; }
    public bool IsPublished { get; set; }
}

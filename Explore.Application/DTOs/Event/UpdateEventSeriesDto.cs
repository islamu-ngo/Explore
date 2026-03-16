using System;

namespace Explore.Application.DTOs.Event;

public class UpdateEventSeriesDto
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? Slug { get; set; }
    public Guid? FeaturedImageId { get; set; }
    public bool IsPublished { get; set; }
}

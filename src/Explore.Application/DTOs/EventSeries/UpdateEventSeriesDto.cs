// ABOUTME: PATCH wrapper DTO for event series property updates using nullable logical groups.
// ABOUTME: Route ID targets the row; groups express independent property update intent.

using Explore.Application.Models.Common;

namespace Explore.Application.DTOs.EventSeries;

public class UpdateEventSeriesDto
{
    public UpdateEventSeriesTitleDto? Title { get; set; }
    public UpdateEventSeriesDescriptionDto? Description { get; set; }
    public UpdateEventSeriesSlugDto? Slug { get; set; }
    public UpdateEventSeriesFeaturedImageDto? FeaturedImage { get; set; }
    public UpdateEventSeriesPublicationDto? Publication { get; set; }
}

public class UpdateEventSeriesTitleDto
{
    public required string Value { get; set; }
}

public class UpdateEventSeriesDescriptionDto
{
    public OptionalUpdate<string?> Value { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public class UpdateEventSeriesSlugDto
{
    public OptionalUpdate<string?> Value { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public class UpdateEventSeriesFeaturedImageDto
{
    public OptionalUpdate<Guid?> Value { get; set; } = OptionalUpdate<Guid?>.Unspecified();
}

public class UpdateEventSeriesPublicationDto
{
    public bool IsPublished { get; set; }
}

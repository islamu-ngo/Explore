// ABOUTME: Grouped update DTO for event-to-category link mutations.
// ABOUTME: Nullable groups allow callers to update the event side or category side independently.

using System;

namespace Explore.Application.DTOs.EventCategories;

public sealed record UpdateEventCategoriesDto
{
    public UpdateEventCategoriesEventDto? Event { get; init; }
    public UpdateEventCategoriesCategoryDto? Category { get; init; }
}

public sealed record UpdateEventCategoriesEventDto
{
    public Guid EventId { get; init; }
}

public sealed record UpdateEventCategoriesCategoryDto
{
    public Guid CategoryId { get; init; }
}

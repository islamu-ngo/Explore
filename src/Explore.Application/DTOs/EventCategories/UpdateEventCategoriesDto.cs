// ABOUTME: Grouped update DTO for event-to-category link mutations.
// ABOUTME: Nullable groups allow callers to update the event side or category side independently.

using System;

namespace Explore.Application.DTOs.EventCategories;

public class UpdateEventCategoriesDto
{
    public UpdateEventCategoriesEventDto? Event { get; set; }
    public UpdateEventCategoriesCategoryDto? Category { get; set; }
}

public class UpdateEventCategoriesEventDto
{
    public Guid EventId { get; set; }
}

public class UpdateEventCategoriesCategoryDto
{
    public Guid CategoryId { get; set; }
}

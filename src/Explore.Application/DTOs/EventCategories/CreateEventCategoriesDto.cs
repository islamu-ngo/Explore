using System;

namespace Explore.Application.DTOs.EventCategories;

public sealed record CreateEventCategoriesDto
{
    public Guid EventId { get; init; }
    public Guid CategoryId { get; init; }
    public Guid TenantId { get; init; }
}

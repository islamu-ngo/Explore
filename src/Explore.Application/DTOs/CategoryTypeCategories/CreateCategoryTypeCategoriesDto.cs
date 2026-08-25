using System;

namespace Explore.Application.DTOs.CategoryTypeCategories;

public sealed record CreateCategoryTypeCategoriesDto
{
    public Guid CategoryId { get; init; }
    public int CategoryTypeId { get; init; }
    public Guid TenantId { get; init; }
}

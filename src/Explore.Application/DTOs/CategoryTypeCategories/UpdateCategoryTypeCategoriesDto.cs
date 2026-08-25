// ABOUTME: Grouped Application-only update contract for a category-to-category-type relationship.
// ABOUTME: Keeps junction identity server-owned while allowing either relationship endpoint to change.

namespace Explore.Application.DTOs.CategoryTypeCategories;

public sealed record UpdateCategoryTypeCategoriesDto
{
    public UpdateCategoryTypeCategoriesRelationshipDto? Relationship { get; init; }
}

public sealed record UpdateCategoryTypeCategoriesRelationshipDto
{
    public Guid? CategoryId { get; init; }
    public int? CategoryTypeId { get; init; }
}

// ABOUTME: Grouped Application-only update contract for a category-to-category-type relationship.
// ABOUTME: Keeps junction identity server-owned while allowing either relationship endpoint to change.

namespace Explore.Application.DTOs.CategoryTypeCategories;

public class UpdateCategoryTypeCategoriesDto
{
    public UpdateCategoryTypeCategoriesRelationshipDto? Relationship { get; set; }
}

public sealed class UpdateCategoryTypeCategoriesRelationshipDto
{
    public Guid? CategoryId { get; set; }
    public int? CategoryTypeId { get; set; }
}

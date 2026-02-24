using Explore.Application.DTOs.Category;

namespace Explore.Application.DTOs.CategoryType;

public class CategoryTypeWithCategoriesDto
{
    public int? Id { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
    public List<CategoryListDto> Categories { get; set; } = [];
}

using Explore.Application.DTOs.Category;

namespace Explore.Application.DTOs.CategoryType;

public sealed record CategoryTypeWithCategoriesDto
{
    public int? Id { get; init; }
    public required string FullName { get; init; }
    public string? Description { get; init; }
    public List<CategoryListDto> Categories { get; init; } = [];
}

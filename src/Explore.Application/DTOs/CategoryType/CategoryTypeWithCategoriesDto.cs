using System.Collections.Immutable;
using Explore.Application.DTOs.Category;

namespace Explore.Application.DTOs.CategoryType;

public sealed record CategoryTypeWithCategoriesDto
{
    public int? Id { get; init; }
    public required string FullName { get; init; }
    public string? Description { get; init; }
    private IReadOnlyList<CategoryListDto>? _categories = ImmutableArray<CategoryListDto>.Empty;

    public IReadOnlyList<CategoryListDto> Categories
    {
        get => _categories!;
        init => _categories = value?.ToImmutableArray();
    }
}

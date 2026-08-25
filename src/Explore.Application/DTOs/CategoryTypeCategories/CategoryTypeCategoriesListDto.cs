using System;

namespace Explore.Application.DTOs.CategoryTypeCategories;

public sealed record CategoryTypeCategoriesListDto
{
    public Guid Id { get; init; }
    public Guid CategoryId { get; init; }
    public string? CategoryFullName { get; init; }
    public string? CategoryMasterCode { get; init; }
    public int CategoryTypeId { get; init; }
    public string? CategoryTypeFullName { get; init; }
    public string? CategoryTypeMasterCode { get; init; }
}

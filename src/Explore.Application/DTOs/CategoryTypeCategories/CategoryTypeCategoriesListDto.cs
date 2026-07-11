using System;

namespace Explore.Application.DTOs.CategoryTypeCategories;

public class CategoryTypeCategoriesListDto
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string? CategoryFullName { get; set; }
    public string? CategoryMasterCode { get; set; }
    public int CategoryTypeId { get; set; }
    public string? CategoryTypeFullName { get; set; }
    public string? CategoryTypeMasterCode { get; set; }
}

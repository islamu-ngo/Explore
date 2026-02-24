using System;

namespace Explore.Application.DTOs.CategoryTypeCategories;

public class CreateCategoryTypeCategoriesDto
{
    public Guid CategoryId { get; set; }
    public int CategoryTypeId { get; set; }
    public Guid TenantId { get; set; }
}

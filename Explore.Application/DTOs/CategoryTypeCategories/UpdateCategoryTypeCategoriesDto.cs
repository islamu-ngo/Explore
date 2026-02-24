using System;

namespace Explore.Application.DTOs.CategoryTypeCategories;

public class UpdateCategoryTypeCategoriesDto
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public int CategoryTypeId { get; set; }
}

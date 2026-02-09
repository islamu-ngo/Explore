using System;

namespace Explore.Application.DTOs.Category;

public class CategoryListDto
{
    public Guid Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public Guid? ParentId { get; set; }
    public string? ParentFullName { get; set; }
}

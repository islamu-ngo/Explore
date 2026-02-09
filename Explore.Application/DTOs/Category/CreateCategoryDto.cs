using System;

namespace Explore.Application.DTOs.Category;

public class CreateCategoryDto
{
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public Guid? ParentId { get; set; }
    public Guid TenantId { get; set; }
}

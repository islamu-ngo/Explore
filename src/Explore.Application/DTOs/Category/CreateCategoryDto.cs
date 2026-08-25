using System;

namespace Explore.Application.DTOs.Category;

public sealed record CreateCategoryDto
{
    public required string MasterCode { get; init; }
    public required string FullName { get; init; }
    public Guid? ParentId { get; init; }
}

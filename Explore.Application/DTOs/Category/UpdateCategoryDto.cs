// ABOUTME: Wrapper DTO for partial category updates using nullable property groups.
// ABOUTME: Body IDs and tenant IDs are absent because PATCH routes use route/context authority.

using Explore.Application.Models.Common;

namespace Explore.Application.DTOs.Category;

public class UpdateCategoryDto
{
    public UpdateCategoryMasterCodeDto? MasterCode { get; set; }
    public UpdateCategoryFullNameDto? FullName { get; set; }
    public UpdateCategoryParentDto? Parent { get; set; }
}

public class UpdateCategoryMasterCodeDto
{
    public required string Value { get; set; }
}

public class UpdateCategoryFullNameDto
{
    public required string Value { get; set; }
}

public class UpdateCategoryParentDto
{
    public OptionalUpdate<Guid?> ParentId { get; set; } = OptionalUpdate<Guid?>.Unspecified();
}

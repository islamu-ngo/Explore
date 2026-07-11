// ABOUTME: Wrapper DTO for PATCH-based Group profile and hierarchy updates using nullable logical groups.
// ABOUTME: Route ID owns identity; nullable fields and relationships use OptionalUpdate for explicit clear semantics.

using System;
using Explore.Application.Models.Common;

namespace Explore.Application.DTOs.Group;

public class UpdateGroupDto
{
    public UpdateGroupFullNameDto? FullName { get; set; }
    public UpdateGroupDescriptionDto? Description { get; set; }
    public UpdateGroupParentOrganizationDto? ParentOrganization { get; set; }
    public UpdateGroupParentGroupDto? ParentGroup { get; set; }
}

public class UpdateGroupFullNameDto
{
    public required string Value { get; set; }
}

public class UpdateGroupDescriptionDto
{
    public OptionalUpdate<string?> Value { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public class UpdateGroupParentOrganizationDto
{
    public OptionalUpdate<Guid?> Value { get; set; } = OptionalUpdate<Guid?>.Unspecified();
}

public class UpdateGroupParentGroupDto
{
    public OptionalUpdate<Guid?> Value { get; set; } = OptionalUpdate<Guid?>.Unspecified();
}

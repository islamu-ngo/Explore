using System;

namespace Explore.Application.DTOs.Group;

public sealed record CreateGroupDto
{
    public required string FullName { get; init; }
    public string? Description { get; init; }
    public Guid? ProfilePictureId { get; init; }
    public Guid? ParentOrganizationId { get; init; }
    public Guid? ParentGroupId { get; init; }
}

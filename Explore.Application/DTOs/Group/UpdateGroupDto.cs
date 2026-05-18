using System;

namespace Explore.Application.DTOs.Group;

public class UpdateGroupDto
{
    public required string FullName { get; set; }
    public string? Description { get; set; }
    public Guid? ParentOrganizationId { get; set; }
    public Guid? ParentGroupId { get; set; }
}

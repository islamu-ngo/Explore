using System;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.GroupMember;

public class UpdateGroupMemberRoleDto
{
    public Guid Id { get; set; }
    public RoleEnum Role { get; set; }
}

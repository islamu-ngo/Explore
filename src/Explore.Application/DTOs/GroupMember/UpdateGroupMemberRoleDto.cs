using System;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.GroupMember;

public sealed record UpdateGroupMemberRoleDto
{
    public Guid Id { get; init; }
    public RoleEnum Role { get; init; }
}

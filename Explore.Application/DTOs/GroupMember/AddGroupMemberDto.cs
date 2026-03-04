// ABOUTME: Input DTO for adding a member to a group by email.
// ABOUTME: Includes optional GroupPositionId to assign a position on creation.

using System;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.GroupMember;

public class AddGroupMemberDto
{
    public Guid GroupId { get; set; }
    public required string Email { get; set; }
    public RoleEnum Role { get; set; } = RoleEnum.GroupMember;
    public int? GroupPositionId { get; set; }
}

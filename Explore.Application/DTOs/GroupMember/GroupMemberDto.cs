using System;

namespace Explore.Application.DTOs.GroupMember;

public class GroupMemberDto
{
    public Guid Id { get; set; }

    // Group
    public Guid GroupId { get; set; }
    public string? GroupFullName { get; set; }

    // User
    public Guid UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserFullName { get; set; }

    // Role
    public int RoleId { get; set; }
    public string? RoleName { get; set; }
}

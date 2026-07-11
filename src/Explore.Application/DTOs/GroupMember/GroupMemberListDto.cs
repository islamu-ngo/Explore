// ABOUTME: List projection DTO for GroupMember with role and position info.
// ABOUTME: Includes GroupPosition fields to match detail DTO pattern.

using System;

namespace Explore.Application.DTOs.GroupMember;

public class GroupMemberListDto
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string? GroupFullName { get; set; }
    public Guid UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserFullName { get; set; }
    public int RoleId { get; set; }
    public string? RoleName { get; set; }

    // Position
    public int? GroupPositionId { get; set; }
    public string? GroupPositionFullName { get; set; }
}

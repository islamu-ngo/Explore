// ABOUTME: List projection DTO for GroupMember with role and position info.
// ABOUTME: Includes GroupPosition fields to match detail DTO pattern.

using System;

namespace Explore.Application.DTOs.GroupMember;

public sealed record GroupMemberListDto
{
    public Guid Id { get; init; }
    public Guid GroupId { get; init; }
    public string? GroupFullName { get; init; }
    public Guid UserId { get; init; }
    public string? UserEmail { get; init; }
    public string? UserFullName { get; init; }
    public int RoleId { get; init; }
    public string? RoleName { get; init; }

    // Position
    public int? GroupPositionId { get; init; }
    public string? GroupPositionFullName { get; init; }
}

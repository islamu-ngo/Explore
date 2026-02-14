using System;

namespace Explore.Application.DTOs.OrganizationMember;

public class OrganizationMemberDto
{
    public Guid Id { get; set; }

    // Organization
    public Guid OrganizationId { get; set; }
    public string? OrganizationFullName { get; set; }

    // User
    public Guid UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserFullName { get; set; }

    // Role
    public int RoleId { get; set; }
    public string? RoleName { get; set; }

    // Position
    public int? OrganizationPositionId { get; set; }
    public string? OrganizationPositionFullName { get; set; }
}

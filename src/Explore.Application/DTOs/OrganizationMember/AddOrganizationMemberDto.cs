using System;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.OrganizationMember;

public sealed record AddOrganizationMemberDto
{
    public Guid OrganizationId { get; init; }
    public required string Email { get; init; }
    public RoleEnum Role { get; init; } = RoleEnum.OrgMember;
}

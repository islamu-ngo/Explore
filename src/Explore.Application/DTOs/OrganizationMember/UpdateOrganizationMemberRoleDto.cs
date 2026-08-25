using System;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.OrganizationMember;

public sealed record UpdateOrganizationMemberRoleDto
{
    public Guid Id { get; init; } // Member ID
    public RoleEnum Role { get; init; }
}

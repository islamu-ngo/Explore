using System;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.OrganizationMember;

public class UpdateOrganizationMemberRoleDto
{
    public Guid Id { get; set; } // Member ID
    public RoleEnum Role { get; set; }
}

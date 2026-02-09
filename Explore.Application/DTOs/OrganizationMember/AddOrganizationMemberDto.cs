using System;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.OrganizationMember;

public class AddOrganizationMemberDto
{
    public Guid OrganizationId { get; set; }
    public required string Email { get; set; }
    public OrganizationRoleEnum Role { get; set; } = OrganizationRoleEnum.Member;
}

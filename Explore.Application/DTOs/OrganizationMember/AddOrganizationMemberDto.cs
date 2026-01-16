using Explore.Domain.Enums;
using System;

namespace Explore.Application.DTOs.OrganizationMember
{
    public class AddOrganizationMemberDto
    {
        public Guid OrganizationId { get; set; }
        public string Email { get; set; }
        public OrganizationRoleEnum Role { get; set; } = OrganizationRoleEnum.Member;
    }
}

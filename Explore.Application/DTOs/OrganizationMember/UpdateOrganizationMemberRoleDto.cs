using Explore.Domain.Enums;
using System;

namespace Explore.Application.DTOs.OrganizationMember
{
    public class UpdateOrganizationMemberRoleDto
    {
        public Guid Id { get; set; } // Member ID
        public OrganizationRoleEnum Role { get; set; }
    }
}

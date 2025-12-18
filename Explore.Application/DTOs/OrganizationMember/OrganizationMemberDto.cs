using Explore.Domain.Enums;
using System;

namespace Explore.Application.DTOs.OrganizationMember
{
    public class OrganizationMemberDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid OrganizationId { get; set; }
        public OrganizationRoleEnum Role { get; set; }
        public string Email { get; set; }
        public string? UserName { get; set; }
    }
}

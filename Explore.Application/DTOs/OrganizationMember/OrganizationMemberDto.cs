using Explore.Domain.Enums;
using System;

namespace Explore.Application.DTOs.OrganizationMember
{
    public class OrganizationMemberDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid OrganizationId { get; set; }
        public OrganizationRole Role { get; set; }
        public string Email { get; set; }
    }
}

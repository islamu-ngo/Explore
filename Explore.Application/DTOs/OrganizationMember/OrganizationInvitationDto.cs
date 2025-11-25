using Explore.Application.DTOs.Organization;
using Explore.Domain.Enums;
using System;

namespace Explore.Application.DTOs.OrganizationMember
{
    public class OrganizationInvitationDto
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public string OrganizationName { get; set; }
        public OrganizationRole Role { get; set; }
        public string Email { get; set; }
    }
}

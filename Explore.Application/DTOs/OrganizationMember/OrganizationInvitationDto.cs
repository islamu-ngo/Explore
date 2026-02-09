using System;
using Explore.Application.DTOs.Organization;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.OrganizationMember;

public class OrganizationInvitationDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string OrganizationName { get; set; }
    public OrganizationRoleEnum Role { get; set; }
    public required string Email { get; set; }
}

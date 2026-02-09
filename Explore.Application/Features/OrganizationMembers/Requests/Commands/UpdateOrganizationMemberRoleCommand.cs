using System;
using Explore.Application.DTOs.OrganizationMember;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.OrganizationMembers.Requests.Commands;

public class UpdateOrganizationMemberRoleCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required UpdateOrganizationMemberRoleDto UpdateOrganizationMemberRoleDto { get; set; }
    public required string RequesterUserId { get; set; }
}

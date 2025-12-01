using Explore.Application.DTOs.OrganizationMember;
using Explore.Application.Responses;
using MediatR;
using System;

namespace Explore.Application.Features.OrganizationMembers.Requests.Commands
{
    public class UpdateOrganizationMemberRoleCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public UpdateOrganizationMemberRoleDto UpdateOrganizationMemberRoleDto { get; set; }
        public string RequesterUserId { get; set; }
    }
}

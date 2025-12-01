using Explore.Application.DTOs.OrganizationMember;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.OrganizationMembers.Requests.Commands
{
    public class AddOrganizationMemberCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public AddOrganizationMemberDto AddOrganizationMemberDto { get; set; }
        public string RequesterUserId { get; set; } // To check permissions
    }
}

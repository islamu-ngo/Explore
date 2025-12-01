using Explore.Application.Responses;
using MediatR;
using System;

namespace Explore.Application.Features.OrganizationMembers.Requests.Commands
{
    public class DeleteOrganizationMemberCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public Guid MemberId { get; set; }
        public string RequesterUserId { get; set; }
    }
}

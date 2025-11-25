using Explore.Application.Responses;
using MediatR;
using System;

namespace Explore.Application.Features.OrganizationMembers.Requests.Commands
{
    public class AcceptInvitationCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public Guid InvitationId { get; set; }
        public Guid UserId { get; set; }
    }
}

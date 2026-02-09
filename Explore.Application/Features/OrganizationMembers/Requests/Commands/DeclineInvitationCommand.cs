using System;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.OrganizationMembers.Requests.Commands;

public class DeclineInvitationCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid InvitationId { get; set; }
    public Guid UserId { get; set; } // To verify ownership if needed, though ID should be enough if we trust the caller or check email
}

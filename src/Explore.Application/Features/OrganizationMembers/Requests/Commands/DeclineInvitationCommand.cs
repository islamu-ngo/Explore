// ABOUTME: MediatR command for declining an organization invitation.
// ABOUTME: Carries the invitation ID and current authenticated user ID.
using System;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.OrganizationMembers.Requests.Commands;

public class DeclineInvitationCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid InvitationId { get; set; }
    public Guid UserId { get; set; }
}

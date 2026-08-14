// ABOUTME: MediatR command for accepting an organization invitation.
// ABOUTME: Carries the invitation ID and current authenticated user ID.
using System;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.OrganizationMembers.Requests.Commands;

public class AcceptInvitationCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid InvitationId { get; set; }
    public Guid UserId { get; set; }
}

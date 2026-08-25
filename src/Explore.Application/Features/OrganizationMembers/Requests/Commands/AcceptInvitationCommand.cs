// ABOUTME: MediatR command for accepting an organization invitation.
// ABOUTME: Carries the invitation ID and current authenticated user ID.
using System;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.OrganizationMembers.Requests.Commands;

public sealed record AcceptInvitationCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid InvitationId { get; init; }
    public Guid UserId { get; init; }
}

// ABOUTME: Command for revoking an event role assignment while preserving audit history.
// ABOUTME: Enforces last-owner protection and same-event authority ceiling.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventRoleAssignments.Requests.Commands;

public sealed class RevokeEventRoleAssignmentCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid ActorUserId { get; set; }
}

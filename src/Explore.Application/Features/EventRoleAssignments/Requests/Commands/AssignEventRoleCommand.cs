// ABOUTME: Command for assigning a first-release operational role to a user for one event.
// ABOUTME: Enforces same-event authority ceiling through the handler before persisting the assignment.

using Explore.Application.Responses;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.EventRoleAssignments.Requests.Commands;

public sealed class AssignEventRoleCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public Guid TargetUserId { get; set; }
    public int RoleId { get; set; }
    public Guid ActorUserId { get; set; }
    public EventRoleAssignmentStatus Status { get; set; } = EventRoleAssignmentStatus.Active;
    public DateTime StartsAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAtUtc { get; set; }
}

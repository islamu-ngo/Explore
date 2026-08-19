// ABOUTME: Command for assigning a first-release operational role to a user for one event.
// ABOUTME: Enforces same-event authority ceiling through the handler before persisting the assignment.

using Explore.Application.Responses;
using Explore.Application.Authorization;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.EventRoleAssignments.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ManageTeam)]
public sealed class AssignEventRoleCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public Guid TargetUserId { get; set; }
    public int RoleId { get; set; }
    public Guid ActorUserId { get; set; }
    public EventRoleAssignmentStatus Status { get; set; } = EventRoleAssignmentStatus.Active;
    public DateTime StartsAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAtUtc { get; set; }

    string? ISecureRequest.ResourceId => EventId == Guid.Empty ? null : EventId.ToString("D");

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        EventId == Guid.Empty
        ? null
        : new EventScopedAuthorizationFacts(TenantId, EventId);
}

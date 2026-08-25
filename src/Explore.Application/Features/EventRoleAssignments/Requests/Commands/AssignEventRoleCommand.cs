// ABOUTME: Command for assigning a first-release operational role to a user for one event.
// ABOUTME: Enforces same-event authority ceiling through the handler before persisting the assignment.

using Explore.Application.Responses;
using Explore.Application.Authorization;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.EventRoleAssignments.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ManageTeam)]
public sealed record AssignEventRoleCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; init; }
    public Guid EventId { get; init; }
    public Guid TargetUserId { get; init; }
    public int RoleId { get; init; }
    public Guid ActorUserId { get; init; }
    public EventRoleAssignmentStatus Status { get; init; } = EventRoleAssignmentStatus.Active;
    public DateTime StartsAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? ExpiresAtUtc { get; init; }

    string? ISecureRequest.ResourceId => EventId == Guid.Empty ? null : EventId.ToString("D");

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        EventId == Guid.Empty
        ? null
        : new EventScopedAuthorizationFacts(TenantId, EventId);
}

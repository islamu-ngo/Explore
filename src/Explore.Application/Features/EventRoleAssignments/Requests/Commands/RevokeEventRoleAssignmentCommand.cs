// ABOUTME: Command for revoking an event role assignment while preserving audit history.
// ABOUTME: Enforces last-owner protection and same-event authority ceiling.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using Explore.Domain.Constants;
using MediatR;

namespace Explore.Application.Features.EventRoleAssignments.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ManageTeam)]
public sealed class RevokeEventRoleAssignmentCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid ActorUserId { get; set; }

    string? ISecureRequest.ResourceId => EventId == Guid.Empty ? null : EventId.ToString("D");

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        EventId == Guid.Empty
        ? null
        : new EventScopedAuthorizationFacts(TenantId, EventId);
}

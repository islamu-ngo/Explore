// ABOUTME: Command for updating the validity window of an open event role assignment.
// ABOUTME: Uses domain lifecycle methods so the app-managed Version concurrency token advances deterministically.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using Explore.Domain.Constants;
using MediatR;

namespace Explore.Application.Features.EventRoleAssignments.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ManageTeam)]
public sealed class UpdateEventRoleAssignmentWindowCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }

    string? ISecureRequest.ResourceId => EventId == Guid.Empty ? null : EventId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => EventId == Guid.Empty
        ? null
        : new Dictionary<string, object>
        {
            ["tenantId"] = TenantId.ToString("D"),
            ["eventId"] = EventId.ToString("D")
        };
}

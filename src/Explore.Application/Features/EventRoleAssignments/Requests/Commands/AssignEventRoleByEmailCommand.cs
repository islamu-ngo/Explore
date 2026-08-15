// ABOUTME: API-facing command for assigning an event role by target user email.
// ABOUTME: Resolves the user in Application before delegating to the canonical assignment command.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.EventRoleAssignments.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ManageTeam)]
public sealed class AssignEventRoleByEmailCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public string TargetUserEmail { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public Guid ActorUserId { get; set; }
    public EventRoleAssignmentStatus Status { get; set; } = EventRoleAssignmentStatus.Active;
    public DateTime StartsAtUtc { get; set; } = DateTime.UtcNow;
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

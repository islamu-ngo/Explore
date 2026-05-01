// ABOUTME: Command for updating the validity window of an open event role assignment.
// ABOUTME: Uses domain lifecycle methods so the app-managed Version concurrency token advances deterministically.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventRoleAssignments.Requests.Commands;

public sealed class UpdateEventRoleAssignmentWindowCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
}

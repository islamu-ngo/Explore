// ABOUTME: First-class command for transferring direct EventOwner authority to another user.
// ABOUTME: Creates the new owner assignment and revokes the previous owner in one transaction.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventRoleAssignments.Requests.Commands;

public sealed class TransferEventOwnershipCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public Guid CurrentOwnerAssignmentId { get; set; }
    public Guid NewOwnerUserId { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTime StartsAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAtUtc { get; set; }
}

// ABOUTME: Command to archive or unarchive a single notification for the authenticated user.
// ABOUTME: Idempotent — archiving an already-archived notification succeeds silently.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Notifications.Requests.Commands;

public class ArchiveNotificationCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid Id { get; set; }
    public bool Archive { get; set; } = true;
}

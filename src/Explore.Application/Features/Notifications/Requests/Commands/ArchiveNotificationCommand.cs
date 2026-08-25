// ABOUTME: Command to archive or unarchive a single notification for the authenticated user.
// ABOUTME: Idempotent — archiving an already-archived notification succeeds silently.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Notifications.Requests.Commands;

public sealed record ArchiveNotificationCommand(Guid Id = default, bool Archive = true) : IRequest<BaseCommandResponse<Guid>>;

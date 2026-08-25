// ABOUTME: Command to mark a single notification as read for the authenticated user.
// ABOUTME: Idempotent — marking an already-read notification succeeds silently.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Notifications.Requests.Commands;

public sealed record MarkNotificationAsReadCommand(Guid Id = default) : IRequest<BaseCommandResponse<Guid>>;

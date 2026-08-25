// ABOUTME: Command to bulk mark all unread notifications as read (YouTube-style).
// ABOUTME: Uses timestamp cutoff to prevent marking newly arrived notifications.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Notifications.Requests.Commands;

public sealed record MarkAllNotificationsAsReadCommand : IRequest<BaseCommandResponse<Guid>>
{
}

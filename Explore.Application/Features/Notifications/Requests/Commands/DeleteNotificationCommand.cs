// ABOUTME: Command to soft-delete a notification for the authenticated user.
// ABOUTME: Handler verifies the notification belongs to the user before deletion.

using MediatR;

namespace Explore.Application.Features.Notifications.Requests.Commands;

public class DeleteNotificationCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

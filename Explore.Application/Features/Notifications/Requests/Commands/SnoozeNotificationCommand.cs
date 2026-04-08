// ABOUTME: Command to snooze a notification until a specified time for the authenticated user.
// ABOUTME: Pass null SnoozedUntil to unsnooze.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Notifications.Requests.Commands;

public class SnoozeNotificationCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid Id { get; set; }
    public DateTime? SnoozedUntil { get; set; }
}

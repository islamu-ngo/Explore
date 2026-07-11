// ABOUTME: Handles bulk marking all unread notifications as read (YouTube-style).
// ABOUTME: Uses timestamp cutoff to prevent race conditions with newly arrived notifications.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Notifications.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Notifications.Handlers.Commands;

public class MarkAllNotificationsAsReadCommandHandler : IRequestHandler<MarkAllNotificationsAsReadCommand, BaseCommandResponse<Guid>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;

    public MarkAllNotificationsAsReadCommandHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUserService)
    {
        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(MarkAllNotificationsAsReadCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            response.Success = false;
            response.Message = "User not authenticated.";
            return response;
        }

        var cutoff = DateTime.UtcNow;
        var count = await _notificationRepository.MarkAllAsRead(userId.Value, cutoff);

        response.Success = true;
        response.Message = $"{count} notification(s) marked as read.";
        return response;
    }
}

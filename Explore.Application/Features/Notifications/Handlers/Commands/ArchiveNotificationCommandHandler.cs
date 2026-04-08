// ABOUTME: Handles archiving or unarchiving a single notification for the authenticated user.
// ABOUTME: Idempotent — archiving an already-archived notification succeeds silently.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Notifications.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Notifications.Handlers.Commands;

public class ArchiveNotificationCommandHandler : IRequestHandler<ArchiveNotificationCommand, BaseCommandResponse<Guid>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;

    public ArchiveNotificationCommandHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUserService)
    {
        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(ArchiveNotificationCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            response.Success = false;
            response.Message = "User not authenticated.";
            return response;
        }

        var result = await _notificationRepository.ArchiveNotification(request.Id, userId.Value, request.Archive);
        if (!result)
        {
            response.Success = false;
            response.Message = "Notification not found.";
            return response;
        }

        response.Success = true;
        response.Id = request.Id;
        response.Message = request.Archive ? "Notification archived." : "Notification unarchived.";
        return response;
    }
}

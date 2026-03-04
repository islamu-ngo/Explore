// ABOUTME: Handles paginated retrieval of notifications for the authenticated user.
// ABOUTME: Supports optional filtering by read status and notification type.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Notification;
using Explore.Application.Features.Notifications.Requests.Queries;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Notifications.Handlers.Queries;

public class GetUserNotificationsRequestHandler : IRequestHandler<GetUserNotificationsRequest, PaginatedResult<NotificationListDto>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetUserNotificationsRequestHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<NotificationListDto>> Handle(GetUserNotificationsRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return PaginatedResult<NotificationListDto>.Create([], 0, request.PageNumber, request.PageSize);

        var (pageNumber, pageSize) = PaginatedResult<NotificationListDto>.NormalizeParameters(request.PageNumber, request.PageSize);

        var (items, totalCount) = await _notificationRepository.GetUserNotificationsPaged(
            userId.Value, pageNumber, pageSize, request.IsRead, request.NotificationTypeId, request.NotificationScopeId);

        var dtos = _mapper.Map<List<NotificationListDto>>(items);

        return PaginatedResult<NotificationListDto>.Create(dtos, totalCount, pageNumber, pageSize);
    }
}

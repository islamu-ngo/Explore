// ABOUTME: Unit tests for GetUserNotificationsRequestHandler.
// ABOUTME: Tests paginated retrieval with user scoping and filtering.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Notification;
using Explore.Application.Features.Notifications.Handlers.Queries;
using Explore.Application.Features.Notifications.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Notifications.Queries;

public class GetUserNotificationsRequestHandlerTests
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly GetUserNotificationsRequestHandler _handler;

    public GetUserNotificationsRequestHandlerTests()
    {
        _notificationRepository = Substitute.For<INotificationRepository>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _mapper = Substitute.For<IMapper>();

        _handler = new GetUserNotificationsRequestHandler(
            _notificationRepository,
            _currentUserService,
            _mapper);
    }

    [Test]
    public async Task Handle_WithAuthenticatedUser_ReturnsPagedNotifications()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);

        var notifications = new List<Notification>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, NotificationTypeId = (int)NotificationTypeEnum.EventCreated, NotificationScopeId = (int)ActorTypeEnum.User, Title = "New Event", DeduplicationKey = "user-notifications-test-1", User = null!, Tenant = null!, NotificationType = null!, NotificationScope = null! },
            new() { Id = Guid.NewGuid(), UserId = userId, NotificationTypeId = (int)NotificationTypeEnum.RegistrationConfirmed, NotificationScopeId = (int)ActorTypeEnum.User, Title = "Registration Confirmed", DeduplicationKey = "user-notifications-test-2", User = null!, Tenant = null!, NotificationType = null!, NotificationScope = null! }
        };
        var dtos = notifications.Select(n => new NotificationListDto
        {
            Id = n.Id,
            NotificationTypeId = n.NotificationTypeId,
            Title = n.Title
        }).ToList();

        _notificationRepository.GetUserNotificationsPaged(userId, 1, 20, null, null, null)
            .Returns((notifications, 2));
        _mapper.Map<List<NotificationListDto>>(notifications).Returns(dtos);

        var request = new GetUserNotificationsRequest { PageNumber = 1, PageSize = 20 };

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result.Items).Count().IsEqualTo(2);
        await Assert.That(result.TotalCount).IsEqualTo(2);
        await Assert.That(result.PageNumber).IsEqualTo(1);
    }

    [Test]
    public async Task Handle_WithNoUser_ReturnsEmptyResult()
    {
        // Arrange
        _currentUserService.UserId.Returns((Guid?)null);
        var request = new GetUserNotificationsRequest { PageNumber = 1, PageSize = 20 };

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result.Items).Count().IsEqualTo(0);
        await Assert.That(result.TotalCount).IsEqualTo(0);
    }

    [Test]
    public async Task Handle_WithIsReadFilter_PassesFilterToRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);
        _notificationRepository.GetUserNotificationsPaged(userId, 1, 20, false, null, null)
            .Returns((new List<Notification>(), 0));
        _mapper.Map<List<NotificationListDto>>(Arg.Any<List<Notification>>())
            .Returns(new List<NotificationListDto>());

        var request = new GetUserNotificationsRequest { PageNumber = 1, PageSize = 20, IsRead = false };

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        await _notificationRepository.Received(1)
            .GetUserNotificationsPaged(userId, 1, 20, false, null, null);
    }
}

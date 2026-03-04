// ABOUTME: Unit tests for DeleteNotificationCommandHandler.
// ABOUTME: Tests soft-deletion with user ownership verification.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Notifications.Handlers.Commands;
using Explore.Application.Features.Notifications.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Notifications.Commands;

public class DeleteNotificationCommandHandlerTests
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly DeleteNotificationCommandHandler _handler;

    public DeleteNotificationCommandHandlerTests()
    {
        _notificationRepository = Substitute.For<INotificationRepository>();
        _currentUserService = Substitute.For<ICurrentUserService>();

        _handler = new DeleteNotificationCommandHandler(
            _notificationRepository,
            _currentUserService);
    }

    [Test]
    public async Task Handle_WithExistingNotification_DeletesAndReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);

        var notification = new Notification
        {
            Id = notificationId,
            UserId = userId,
            NotificationTypeId = (int)NotificationTypeEnum.EventCreated,
            NotificationScopeId = (int)ActorTypeEnum.User,
            Title = "Test",
            User = null!,
            Tenant = null!,
            NotificationType = null!,
            NotificationScope = null!
        };

        _notificationRepository.GetByIdForUser(notificationId, userId).Returns(notification);

        var command = new DeleteNotificationCommand { Id = notificationId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result).IsTrue();
        await _notificationRepository.Received(1).Delete(notification);
    }

    [Test]
    public async Task Handle_WithNonExistentNotification_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);
        _notificationRepository.GetByIdForUser(Arg.Any<Guid>(), userId).Returns((Notification?)null);

        var command = new DeleteNotificationCommand { Id = Guid.NewGuid() };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Handle_WithNoUser_ReturnsFalse()
    {
        // Arrange
        _currentUserService.UserId.Returns((Guid?)null);
        var command = new DeleteNotificationCommand { Id = Guid.NewGuid() };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result).IsFalse();
    }
}

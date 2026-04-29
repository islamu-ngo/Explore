// ABOUTME: Unit tests for idempotent email notification unsubscribe command handling.
// ABOUTME: Verifies unknown categories, new opt-out creation, and existing preference updates.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EmailUnsubscribe.Handlers.Commands;
using Explore.Application.Features.EmailUnsubscribe.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Constants;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EmailUnsubscribe.Commands;

public sealed class UnsubscribeFromEmailCategoryCommandHandlerTests
{
    private readonly IUserNotificationPreferenceRepository _repository;
    private readonly UnsubscribeFromEmailCategoryCommandHandler _handler;

    public UnsubscribeFromEmailCategoryCommandHandlerTests()
    {
        _repository = Substitute.For<IUserNotificationPreferenceRepository>();
        var logger = Substitute.For<ILogger<UnsubscribeFromEmailCategoryCommandHandler>>();
        _handler = new UnsubscribeFromEmailCategoryCommandHandler(_repository, logger);
    }

    [Test]
    public async Task Handle_WithUnknownCategory_ReturnsFailureAndDoesNotPersist()
    {
        var result = await _handler.Handle(new UnsubscribeFromEmailCategoryCommand
        {
            TenantId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Category = "unknown"
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("unknown_notification_category");
        await _repository.DidNotReceive().Create(Arg.Any<UserNotificationPreference>());
        await _repository.DidNotReceive().Update(Arg.Any<UserNotificationPreference>());
    }

    [Test]
    public async Task Handle_WhenPreferenceDoesNotExist_CreatesDisabledPreference()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _repository.GetByUserAndCategory(
                tenantId,
                userId,
                NotificationPreferenceCategories.RegistrationConfirmations)
            .Returns((UserNotificationPreference?)null);

        var result = await _handler.Handle(new UnsubscribeFromEmailCategoryCommand
        {
            TenantId = tenantId,
            UserId = userId,
            Category = NotificationPreferenceCategories.RegistrationConfirmations.ToUpperInvariant()
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _repository.Received(1).Create(Arg.Is<UserNotificationPreference>(preference =>
            preference.TenantId == tenantId
            && preference.UserId == userId
            && preference.Category == NotificationPreferenceCategories.RegistrationConfirmations
            && !preference.IsEnabled));
        await _repository.DidNotReceive().Update(Arg.Any<UserNotificationPreference>());
    }

    [Test]
    public async Task Handle_WhenPreferenceExists_DisablesExistingPreference()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var preference = new UserNotificationPreference
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Tenant = null!,
            UserId = userId,
            Category = NotificationPreferenceCategories.RegistrationConfirmations,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _repository.GetByUserAndCategory(
                tenantId,
                userId,
                NotificationPreferenceCategories.RegistrationConfirmations)
            .Returns(preference);

        var result = await _handler.Handle(new UnsubscribeFromEmailCategoryCommand
        {
            TenantId = tenantId,
            UserId = userId,
            Category = NotificationPreferenceCategories.RegistrationConfirmations
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(preference.IsEnabled).IsFalse();
        await Assert.That(preference.UpdatedBy).IsEqualTo(userId);
        await _repository.Received(1).Update(preference);
        await _repository.DidNotReceive().Create(Arg.Any<UserNotificationPreference>());
    }
}

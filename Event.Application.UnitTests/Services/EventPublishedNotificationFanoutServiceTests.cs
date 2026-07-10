// ABOUTME: Unit tests for event-published notification fanout service idempotency and inbox creation.
// ABOUTME: Verifies subscriber scans create deterministic notification rows and durable fanout progress.

using System.Diagnostics.Metrics;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Models.InternalEvents;
using Explore.Application.Services;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Services;

public sealed class EventPublishedNotificationFanoutServiceTests
{
    private readonly IActorSubscriptionRepository _actorSubscriptionRepository = Substitute.For<IActorSubscriptionRepository>();
    private readonly INotificationRepository _notificationRepository = Substitute.For<INotificationRepository>();
    private readonly INotificationFanoutRunRepository _fanoutRunRepository = Substitute.For<INotificationFanoutRunRepository>();
    private readonly INotificationPreferenceResolver _preferenceResolver = Substitute.For<INotificationPreferenceResolver>();
    private readonly IWebPushSubscriptionRepository _webPushSubscriptionRepository = Substitute.For<IWebPushSubscriptionRepository>();
    private readonly IWebPushDispatchOutboxRepository _webPushDispatchOutboxRepository = Substitute.For<IWebPushDispatchOutboxRepository>();
    private readonly EventPublishedNotificationFanoutService _service;

    public EventPublishedNotificationFanoutServiceTests()
    {
        _service = new EventPublishedNotificationFanoutService(
            _actorSubscriptionRepository,
            _notificationRepository,
            _fanoutRunRepository,
            _preferenceResolver,
            _webPushSubscriptionRepository,
            _webPushDispatchOutboxRepository,
            CreateMetrics(),
            Substitute.For<ILogger<EventPublishedNotificationFanoutService>>());

        _preferenceResolver.ResolveAsync(Arg.Any<NotificationPreferenceResolveRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => EnabledDecision(call.Arg<NotificationPreferenceResolveRequest>()));
        _webPushSubscriptionRepository.ListActiveForUserAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<WebPushSubscription>());
    }

    [Test]
    public async Task FanoutAsync_WithActiveSubscriptions_CreatesNotificationsAndCompletesRun()
    {
        var request = CreateRequest();
        var subscription = CreateSubscription(request.TenantId, request.SourceActorId);
        var createdNotifications = new List<Notification>();
        var updatedRuns = new List<NotificationFanoutRun>();

        _fanoutRunRepository.GetBySourceAsync(
                request.TenantId,
                EventPublishedNotificationFanoutService.FanoutKind,
                (int)NotificationEntityTypeEnum.Event,
                request.EventId,
                request.SourceActorId,
                true,
                Arg.Any<CancellationToken>())
            .Returns((NotificationFanoutRun?)null);
        _fanoutRunRepository.Create(Arg.Any<NotificationFanoutRun>())
            .Returns(call => call.Arg<NotificationFanoutRun>());
        _fanoutRunRepository.Update(Arg.Do<NotificationFanoutRun>(run => updatedRuns.Add(CloneRun(run))))
            .Returns(Task.CompletedTask);
        _actorSubscriptionRepository.GetActiveFanoutBatchAsync(
                request.TenantId,
                request.SourceActorId,
                null,
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([subscription]);
        _notificationRepository.ExistsByDeduplicationKeyAsync(
                request.TenantId,
                subscription.SubscriberUserId,
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(false);
        _notificationRepository.GetByDeduplicationKeyAsync(
                request.TenantId,
                subscription.SubscriberUserId,
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns((Notification?)null);
        _notificationRepository.Create(Arg.Do<Notification>(notification => createdNotifications.Add(notification)))
            .Returns(call => call.Arg<Notification>());

        await _service.FanoutAsync(request);

        await Assert.That(createdNotifications).Count().IsEqualTo(1);
        var notification = createdNotifications.Single();
        await Assert.That(notification.TenantId).IsEqualTo(request.TenantId);
        await Assert.That(notification.UserId).IsEqualTo(subscription.SubscriberUserId);
        await Assert.That(notification.NotificationTypeId).IsEqualTo((int)NotificationTypeEnum.EventCreated);
        await Assert.That(notification.NotificationEntityTypeId).IsEqualTo((int)NotificationEntityTypeEnum.Event);
        await Assert.That(notification.EntityId).IsEqualTo(request.EventId.ToString());
        await Assert.That(notification.NotificationScopeId).IsEqualTo((int)ActorTypeEnum.Organization);
        await Assert.That(notification.SourceActorId).IsEqualTo(request.SourceActorId);
        await Assert.That(notification.RecipientContextActorId).IsEqualTo(subscription.TargetActorId);
        await Assert.That(notification.NotificationReasonId).IsEqualTo((int)NotificationReasonEnum.Subscription);
        await Assert.That(notification.DeduplicationKey).Contains(request.EventId.ToString("N"));
        await Assert.That(updatedRuns.Last().Status).IsEqualTo(EventPublishedNotificationFanoutService.StatusCompleted);
        await Assert.That(updatedRuns.Last().ProcessedCount).IsEqualTo(1);
        await Assert.That(updatedRuns.Last().CreatedNotificationCount).IsEqualTo(1);
        await Assert.That(updatedRuns.Last().CursorSubscriberTenantUserId).IsEqualTo(subscription.SubscriberTenantUserId);
    }

    [Test]
    public async Task FanoutAsync_WithExistingDeduplicationKey_SkipsDuplicateNotification()
    {
        var request = CreateRequest();
        var subscription = CreateSubscription(request.TenantId, request.SourceActorId);

        _fanoutRunRepository.GetBySourceAsync(
                request.TenantId,
                EventPublishedNotificationFanoutService.FanoutKind,
                (int)NotificationEntityTypeEnum.Event,
                request.EventId,
                request.SourceActorId,
                true,
                Arg.Any<CancellationToken>())
            .Returns((NotificationFanoutRun?)null);
        _fanoutRunRepository.Create(Arg.Any<NotificationFanoutRun>())
            .Returns(call => call.Arg<NotificationFanoutRun>());
        _actorSubscriptionRepository.GetActiveFanoutBatchAsync(
                request.TenantId,
                request.SourceActorId,
                null,
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([subscription]);
        _notificationRepository.GetByDeduplicationKeyAsync(
                request.TenantId,
                subscription.SubscriberUserId,
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new Notification
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                Tenant = null!,
                UserId = subscription.SubscriberUserId,
                User = null!,
                NotificationTypeId = (int)NotificationTypeEnum.EventCreated,
                NotificationType = null!,
                NotificationScope = null!,
                Title = "Existing notification",
                Body = "Existing body",
                DeduplicationKey = "existing",
                CreatedAt = DateTime.UtcNow
            });

        await _service.FanoutAsync(request);

        await _notificationRepository.DidNotReceive().Create(Arg.Any<Notification>());
        await _fanoutRunRepository.Received().Update(Arg.Is<NotificationFanoutRun>(run =>
            run.Status == EventPublishedNotificationFanoutService.StatusCompleted
            && run.ProcessedCount == 1
            && run.CreatedNotificationCount == 0));
    }

    [Test]
    public async Task FanoutAsync_WhenMatrixDisablesInAppEventUpdates_SkipsNotificationCreation()
    {
        var request = CreateRequest();
        var subscription = CreateSubscription(request.TenantId, request.SourceActorId);

        _fanoutRunRepository.GetBySourceAsync(
                request.TenantId,
                EventPublishedNotificationFanoutService.FanoutKind,
                (int)NotificationEntityTypeEnum.Event,
                request.EventId,
                request.SourceActorId,
                true,
                Arg.Any<CancellationToken>())
            .Returns((NotificationFanoutRun?)null);
        _fanoutRunRepository.Create(Arg.Any<NotificationFanoutRun>())
            .Returns(call => call.Arg<NotificationFanoutRun>());
        _actorSubscriptionRepository.GetActiveFanoutBatchAsync(
                request.TenantId,
                request.SourceActorId,
                null,
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([subscription]);
        _notificationRepository.ExistsByDeduplicationKeyAsync(
                request.TenantId,
                subscription.SubscriberUserId,
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(false);
        _notificationRepository.GetByDeduplicationKeyAsync(
                request.TenantId,
                subscription.SubscriberUserId,
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns((Notification?)null);
        _preferenceResolver.ResolveAsync(Arg.Any<NotificationPreferenceResolveRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => DisabledDecision(call.Arg<NotificationPreferenceResolveRequest>()));

        await _service.FanoutAsync(request);

        await _notificationRepository.DidNotReceive().Create(Arg.Any<Notification>());
        await _fanoutRunRepository.Received().Update(Arg.Is<NotificationFanoutRun>(run =>
            run.Status == EventPublishedNotificationFanoutService.StatusCompleted
            && run.ProcessedCount == 1
            && run.CreatedNotificationCount == 0));
    }

    [Test]
    public async Task FanoutAsync_WithCompletedRun_DoesNothing()
    {
        var request = CreateRequest();
        _fanoutRunRepository.GetBySourceAsync(
                request.TenantId,
                EventPublishedNotificationFanoutService.FanoutKind,
                (int)NotificationEntityTypeEnum.Event,
                request.EventId,
                request.SourceActorId,
                true,
                Arg.Any<CancellationToken>())
            .Returns(new NotificationFanoutRun
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                Tenant = null!,
                FanoutKind = EventPublishedNotificationFanoutService.FanoutKind,
                NotificationEntityTypeId = (int)NotificationEntityTypeEnum.Event,
                NotificationEntityType = null!,
                EntityId = request.EventId,
                SourceActorId = request.SourceActorId,
                SourceActor = null!,
                Status = EventPublishedNotificationFanoutService.StatusCompleted
            });

        await _service.FanoutAsync(request);

        await _actorSubscriptionRepository.DidNotReceiveWithAnyArgs().GetActiveFanoutBatchAsync(default, default, default, default, default);
        await _notificationRepository.DidNotReceiveWithAnyArgs().Create(default!);
        await _fanoutRunRepository.DidNotReceive().Update(Arg.Any<NotificationFanoutRun>());
    }

    private static EventPublishedNotificationFanoutRequested CreateRequest() => new()
    {
        TenantId = Guid.NewGuid(),
        EventId = Guid.NewGuid(),
        EventTitle = "Community Iftar",
        SourceActorId = Guid.NewGuid(),
        StartDate = DateTimeOffset.UtcNow.AddDays(7),
        PublishedAt = DateTimeOffset.UtcNow
    };

    private static ActorSubscription CreateSubscription(Guid tenantId, Guid targetActorId) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Tenant = null!,
        SubscriberTenantUserId = Guid.NewGuid(),
        SubscriberTenantUser = null!,
        SubscriberUserId = Guid.NewGuid(),
        SubscriberUser = null!,
        TargetActorId = targetActorId,
        TargetActor = null!,
        TargetActorTypeId = (int)ActorTypeEnum.Organization,
        TargetActorType = null!,
        StatusId = (int)ActorSubscriptionStatusEnum.Active,
        Status = null!,
        NotificationLevelId = (int)ActorSubscriptionNotificationLevelEnum.All,
        NotificationLevel = null!,
        SubscribedAt = DateTime.UtcNow
    };

    private static NotificationFanoutRun CloneRun(NotificationFanoutRun run) => new()
    {
        Id = run.Id,
        TenantId = run.TenantId,
        Tenant = null!,
        FanoutKind = run.FanoutKind,
        NotificationEntityTypeId = run.NotificationEntityTypeId,
        NotificationEntityType = null!,
        EntityId = run.EntityId,
        SourceActorId = run.SourceActorId,
        SourceActor = null!,
        Status = run.Status,
        CursorSubscriberTenantUserId = run.CursorSubscriberTenantUserId,
        ProcessedCount = run.ProcessedCount,
        CreatedNotificationCount = run.CreatedNotificationCount,
        StartedAt = run.StartedAt,
        CompletedAt = run.CompletedAt,
        FailedAt = run.FailedAt,
        LastError = run.LastError
    };

    private static BusinessMetrics CreateMetrics()
    {
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));
        return new BusinessMetrics(meterFactory);
    }

    private static NotificationPreferenceDecision EnabledDecision(NotificationPreferenceResolveRequest request)
    {
        return new NotificationPreferenceDecision(
            request.CategoryCode,
            request.ChannelCode,
            true,
            false,
            false,
            false,
            "Default",
            null);
    }

    private static NotificationPreferenceDecision DisabledDecision(NotificationPreferenceResolveRequest request)
    {
        return new NotificationPreferenceDecision(
            request.CategoryCode,
            request.ChannelCode,
            false,
            false,
            false,
            false,
            "User",
            null);
    }
}

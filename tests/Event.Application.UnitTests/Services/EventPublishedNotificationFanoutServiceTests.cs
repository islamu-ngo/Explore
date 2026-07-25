// ABOUTME: Unit tests for event-published notification fanout service idempotency and inbox creation.
// ABOUTME: Verifies subscriber scans create deterministic notification rows and durable fanout progress.

using System.Diagnostics.Metrics;
using System.Security.Cryptography;
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
    private readonly IPrivacyErasureStateRepository _privacyErasureStateRepository = Substitute.For<IPrivacyErasureStateRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
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
            _privacyErasureStateRepository,
            _unitOfWork,
            _preferenceResolver,
            _webPushSubscriptionRepository,
            _webPushDispatchOutboxRepository,
            CreateMetrics(),
            Substitute.For<ILogger<EventPublishedNotificationFanoutService>>());

        _preferenceResolver.ResolveAsync(Arg.Any<NotificationPreferenceResolveRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => EnabledDecision(call.Arg<NotificationPreferenceResolveRequest>()));
        _privacyErasureStateRepository.GetBySubjectAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((PrivacyErasureSaga?)null);
        _unitOfWork.ExecuteSerializableAsync(
                Arg.Any<Func<CancellationToken, Task<int>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<int>>>()(call.Arg<CancellationToken>()));
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
        var firstSubscription = CreateSubscription(request.TenantId, request.SourceActorId);
        var secondSubscription = CreateSubscription(request.TenantId, request.SourceActorId);
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
            .Returns([firstSubscription, secondSubscription]);
        _notificationRepository.ExistsByDeduplicationKeyAsync(
                request.TenantId,
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(false);
        _notificationRepository.GetByDeduplicationKeyAsync(
                request.TenantId,
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns((Notification?)null);
        _notificationRepository.Create(Arg.Do<Notification>(notification => createdNotifications.Add(notification)))
            .Returns(call => call.Arg<Notification>());

        await _service.FanoutAsync(request);

        await Assert.That(createdNotifications).Count().IsEqualTo(2);
        await Assert.That(createdNotifications.Select(notification => notification.UserId))
            .IsEquivalentTo([firstSubscription.SubscriberUserId, secondSubscription.SubscriberUserId]);
        await Assert.That(createdNotifications.All(notification =>
            notification.TenantId == request.TenantId
            && notification.NotificationTypeId == (int)NotificationTypeEnum.EventCreated
            && notification.NotificationEntityTypeId == (int)NotificationEntityTypeEnum.Event
            && notification.EntityId == request.EventId.ToString()
            && notification.NotificationScopeId == (int)ActorTypeEnum.Organization
            && notification.SourceActorId == request.SourceActorId
            && notification.NotificationReasonId == (int)NotificationReasonEnum.Subscription
            && notification.DeduplicationKey.Contains(request.EventId.ToString("N"), StringComparison.Ordinal)))
            .IsTrue();
        await Assert.That(updatedRuns.Last().Status).IsEqualTo(EventPublishedNotificationFanoutService.StatusCompleted);
        await Assert.That(updatedRuns.Last().ProcessedCount).IsEqualTo(2);
        await Assert.That(updatedRuns.Last().CreatedNotificationCount).IsEqualTo(2);
        await Assert.That(updatedRuns.Last().CursorSubscriberTenantUserId).IsEqualTo(secondSubscription.SubscriberTenantUserId);
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
    public async Task FanoutAsync_WithOneFencedAndOneActiveSubscriber_SkipsFencedPiiReadsAndContinues()
    {
        var request = CreateRequest();
        var fencedSubscription = CreateSubscription(request.TenantId, request.SourceActorId);
        var activeSubscription = CreateSubscription(request.TenantId, request.SourceActorId);
        var createdNotifications = new List<Notification>();

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
            .Returns([fencedSubscription, activeSubscription]);
        _privacyErasureStateRepository.GetBySubjectAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Guid>() == fencedSubscription.SubscriberUserId
                ? CreateFencedSaga(fencedSubscription.SubscriberUserId)
                : null);
        _notificationRepository.GetByDeduplicationKeyAsync(
                request.TenantId,
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns((Notification?)null);
        _notificationRepository.Create(Arg.Do<Notification>(createdNotifications.Add))
            .Returns(call => call.Arg<Notification>());

        await _service.FanoutAsync(request);

        await Assert.That(createdNotifications.Select(notification => notification.UserId))
            .IsEquivalentTo([activeSubscription.SubscriberUserId]);
        await _preferenceResolver.DidNotReceive().ResolveAsync(
            Arg.Is<NotificationPreferenceResolveRequest>(item => item.UserId == fencedSubscription.SubscriberUserId),
            Arg.Any<CancellationToken>());
        await _notificationRepository.DidNotReceive().GetByDeduplicationKeyAsync(
            request.TenantId,
            fencedSubscription.SubscriberUserId,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FanoutAsync_WhenFenceAppearsBeforeRecipientWrite_SkipsNotificationAndWebPush()
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
        _privacyErasureStateRepository.GetBySubjectAsync(subscription.SubscriberUserId, Arg.Any<CancellationToken>())
            .Returns((PrivacyErasureSaga?)null, CreateFencedSaga(subscription.SubscriberUserId));
        _notificationRepository.GetByDeduplicationKeyAsync(
                request.TenantId,
                subscription.SubscriberUserId,
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns((Notification?)null);
        _notificationRepository.Create(Arg.Any<Notification>())
            .Returns(call => call.Arg<Notification>());

        await _service.FanoutAsync(request);

        await _privacyErasureStateRepository.Received(2)
            .GetBySubjectAsync(subscription.SubscriberUserId, Arg.Any<CancellationToken>());
        await _notificationRepository.DidNotReceive().Create(Arg.Any<Notification>());
        await _preferenceResolver.DidNotReceive().ResolveAsync(Arg.Any<NotificationPreferenceResolveRequest>(), Arg.Any<CancellationToken>());
        await _webPushSubscriptionRepository.DidNotReceive().ListActiveForUserAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
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

    private static PrivacyErasureSaga CreateFencedSaga(Guid userId)
    {
        DateTime now = DateTime.UtcNow;
        PrivacyErasureIntent intent = PrivacyErasureIntent.Record(
            Guid.CreateVersion7(),
            authoritySequence: 1,
            PrivacyErasureSubjectKind.User,
            userId,
            PrivacyErasureReasonCode.AccountDeletion,
            policyVersion: 1,
            now,
            now);
        return PrivacyErasureSaga.Start(
            intent,
            fenceToken: 1,
            SHA256.HashData([1]),
            now.AddHours(1),
            now);
    }
}

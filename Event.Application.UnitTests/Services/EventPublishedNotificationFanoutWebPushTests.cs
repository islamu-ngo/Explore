// ABOUTME: Unit tests for Web Push dispatch creation from event notification fanout.
// ABOUTME: Proves eligible active push subscriptions receive dispatch rows and opted-out users do not.

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

public sealed class EventPublishedNotificationFanoutWebPushTests
{
    private readonly IActorSubscriptionRepository _actorSubscriptionRepository = Substitute.For<IActorSubscriptionRepository>();
    private readonly INotificationRepository _notificationRepository = Substitute.For<INotificationRepository>();
    private readonly INotificationFanoutRunRepository _fanoutRunRepository = Substitute.For<INotificationFanoutRunRepository>();
    private readonly INotificationPreferenceResolver _preferenceResolver = Substitute.For<INotificationPreferenceResolver>();
    private readonly IWebPushSubscriptionRepository _webPushSubscriptionRepository = Substitute.For<IWebPushSubscriptionRepository>();
    private readonly IWebPushDispatchOutboxRepository _webPushDispatchOutboxRepository = Substitute.For<IWebPushDispatchOutboxRepository>();

    [Test]
    public async Task FanoutAsync_WhenPushEnabled_EnqueuesOneDispatchPerActivePushSubscription()
    {
        var request = CreateRequest();
        var actorSubscription = CreateActorSubscription(request);
        var pushSubscription = WebPushSubscription.Create(request.TenantId, actorSubscription.SubscriberUserId, "device-a", "https://push.example/sub-a", "p256dh", "auth", null, DateTime.UtcNow);
        SetupRunAndSubscriber(request, actorSubscription);
        _preferenceResolver.ResolveAsync(Arg.Any<NotificationPreferenceResolveRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => EnabledDecision(call.Arg<NotificationPreferenceResolveRequest>()));
        _webPushSubscriptionRepository.ListActiveForUserAsync(request.TenantId, actorSubscription.SubscriberUserId, Arg.Any<CancellationToken>())
            .Returns([pushSubscription]);
        var createdDispatches = new List<WebPushDispatchOutbox>();
        _webPushDispatchOutboxRepository.CreateIfNotExistsAsync(Arg.Do<WebPushDispatchOutbox>(createdDispatches.Add), Arg.Any<CancellationToken>())
            .Returns(true);
        var service = CreateService();

        await service.FanoutAsync(request);

        await Assert.That(createdDispatches).Count().IsEqualTo(1);
        await Assert.That(createdDispatches[0].TenantId).IsEqualTo(request.TenantId);
        await Assert.That(createdDispatches[0].UserId).IsEqualTo(actorSubscription.SubscriberUserId);
        await Assert.That(createdDispatches[0].SubscriptionId).IsEqualTo(pushSubscription.Id);
        await Assert.That(createdDispatches[0].CategoryId).IsEqualTo((int)NotificationPreferenceCategoryEnum.EventUpdates);
        await Assert.That(createdDispatches[0].PayloadJson).DoesNotContain(request.EventTitle);
    }

    [Test]
    public async Task FanoutAsync_WhenPushDisabled_DoesNotCreateDispatchRows()
    {
        var request = CreateRequest();
        var actorSubscription = CreateActorSubscription(request);
        SetupRunAndSubscriber(request, actorSubscription);
        _preferenceResolver.ResolveAsync(Arg.Any<NotificationPreferenceResolveRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<NotificationPreferenceResolveRequest>().ChannelCode == NotificationPreferenceChannelCodes.Push
                ? DisabledDecision(call.Arg<NotificationPreferenceResolveRequest>())
                : EnabledDecision(call.Arg<NotificationPreferenceResolveRequest>()));
        var service = CreateService();

        await service.FanoutAsync(request);

        await _webPushSubscriptionRepository.DidNotReceive().ListActiveForUserAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _webPushDispatchOutboxRepository.DidNotReceive().CreateIfNotExistsAsync(Arg.Any<WebPushDispatchOutbox>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FanoutAsync_WhenDispatchFanoutPartiallyFails_RetryRepairsMissingWebPushDispatchWithoutDuplicates()
    {
        var request = CreateRequest();
        var actorSubscription = CreateActorSubscription(request);
        var pushSubscriptionA = WebPushSubscription.Create(request.TenantId, actorSubscription.SubscriberUserId, "device-a", "https://push.example/sub-a", "p256dh-a", "auth-a", null, DateTime.UtcNow);
        var pushSubscriptionB = WebPushSubscription.Create(request.TenantId, actorSubscription.SubscriberUserId, "device-b", "https://push.example/sub-b", "p256dh-b", "auth-b", null, DateTime.UtcNow);
        SetupRunAndSubscriber(request, actorSubscription);
        Notification? createdNotification = null;
        _notificationRepository.GetByDeduplicationKeyAsync(request.TenantId, actorSubscription.SubscriberUserId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => createdNotification);
        _notificationRepository.Create(Arg.Do<Notification>(notification => createdNotification = notification))
            .Returns(call => call.Arg<Notification>());
        _preferenceResolver.ResolveAsync(Arg.Any<NotificationPreferenceResolveRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => EnabledDecision(call.Arg<NotificationPreferenceResolveRequest>()));
        _webPushSubscriptionRepository.ListActiveForUserAsync(request.TenantId, actorSubscription.SubscriberUserId, Arg.Any<CancellationToken>())
            .Returns([pushSubscriptionA, pushSubscriptionB]);
        var persistedSubscriptionIds = new HashSet<Guid>();
        var failSecondDispatchOnce = true;
        _webPushDispatchOutboxRepository.CreateIfNotExistsAsync(Arg.Any<WebPushDispatchOutbox>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var dispatch = call.Arg<WebPushDispatchOutbox>();
                if (dispatch.SubscriptionId == pushSubscriptionB.Id && failSecondDispatchOnce)
                {
                    failSecondDispatchOnce = false;
                    throw new InvalidOperationException("Simulated partial WebPush dispatch fanout failure.");
                }

                return persistedSubscriptionIds.Add(dispatch.SubscriptionId);
            });
        var service = CreateService();

        await Assert.That(async () => await service.FanoutAsync(request)).Throws<InvalidOperationException>();
        await Assert.That(persistedSubscriptionIds).Count().IsEqualTo(1);

        await service.FanoutAsync(request);
        await service.FanoutAsync(request);

        await _notificationRepository.Received(1).Create(Arg.Any<Notification>());
        await Assert.That(createdNotification).IsNotNull();
        await Assert.That(persistedSubscriptionIds).IsEquivalentTo([pushSubscriptionA.Id, pushSubscriptionB.Id]);
    }

    [Test]
    public async Task FanoutAsync_WhenPushOptedOut_DoesNotRepairDispatchForExistingNotification()
    {
        var request = CreateRequest();
        var actorSubscription = CreateActorSubscription(request);
        SetupRunAndSubscriber(request, actorSubscription);
        _notificationRepository.GetByDeduplicationKeyAsync(request.TenantId, actorSubscription.SubscriberUserId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Notification
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                Tenant = null!,
                UserId = actorSubscription.SubscriberUserId,
                User = null!,
                NotificationTypeId = (int)NotificationTypeEnum.EventCreated,
                NotificationType = null!,
                NotificationScope = null!,
                Title = "Existing notification",
                Body = "Existing body",
                DeduplicationKey = "existing",
                CreatedAt = DateTime.UtcNow
            });
        _preferenceResolver.ResolveAsync(Arg.Any<NotificationPreferenceResolveRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<NotificationPreferenceResolveRequest>().ChannelCode == NotificationPreferenceChannelCodes.Push
                ? DisabledDecision(call.Arg<NotificationPreferenceResolveRequest>())
                : EnabledDecision(call.Arg<NotificationPreferenceResolveRequest>()));
        var service = CreateService();

        await service.FanoutAsync(request);

        await _webPushSubscriptionRepository.DidNotReceive().ListActiveForUserAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _webPushDispatchOutboxRepository.DidNotReceive().CreateIfNotExistsAsync(Arg.Any<WebPushDispatchOutbox>(), Arg.Any<CancellationToken>());
    }

    private EventPublishedNotificationFanoutService CreateService()
    {
        return new EventPublishedNotificationFanoutService(
            _actorSubscriptionRepository,
            _notificationRepository,
            _fanoutRunRepository,
            _preferenceResolver,
            _webPushSubscriptionRepository,
            _webPushDispatchOutboxRepository,
            CreateMetrics(),
            Substitute.For<ILogger<EventPublishedNotificationFanoutService>>());
    }

    private void SetupRunAndSubscriber(EventPublishedNotificationFanoutRequested request, ActorSubscription subscription)
    {
        _fanoutRunRepository.GetBySourceAsync(request.TenantId, EventPublishedNotificationFanoutService.FanoutKind, (int)NotificationEntityTypeEnum.Event, request.EventId, request.SourceActorId, true, Arg.Any<CancellationToken>())
            .Returns((NotificationFanoutRun?)null);
        _fanoutRunRepository.Create(Arg.Any<NotificationFanoutRun>()).Returns(call => call.Arg<NotificationFanoutRun>());
        _actorSubscriptionRepository.GetActiveFanoutBatchAsync(request.TenantId, request.SourceActorId, null, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([subscription]);
        _notificationRepository.ExistsByDeduplicationKeyAsync(request.TenantId, subscription.SubscriberUserId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _notificationRepository.GetByDeduplicationKeyAsync(request.TenantId, subscription.SubscriberUserId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Notification?)null);
        _notificationRepository.Create(Arg.Any<Notification>()).Returns(call => call.Arg<Notification>());
    }

    private static EventPublishedNotificationFanoutRequested CreateRequest() => new()
    {
        TenantId = Guid.NewGuid(),
        EventId = Guid.NewGuid(),
        EventTitle = "Private title must stay out of push payload",
        SourceActorId = Guid.NewGuid(),
        StartDate = DateTimeOffset.UtcNow.AddDays(7),
        PublishedAt = DateTimeOffset.UtcNow
    };

    private static ActorSubscription CreateActorSubscription(EventPublishedNotificationFanoutRequested request) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = request.TenantId,
        Tenant = null!,
        SubscriberTenantUserId = Guid.NewGuid(),
        SubscriberTenantUser = null!,
        SubscriberUserId = Guid.NewGuid(),
        SubscriberUser = null!,
        TargetActorId = request.SourceActorId,
        TargetActor = null!,
        TargetActorTypeId = (int)ActorTypeEnum.Organization,
        TargetActorType = null!,
        StatusId = (int)ActorSubscriptionStatusEnum.Active,
        Status = null!,
        NotificationLevelId = (int)ActorSubscriptionNotificationLevelEnum.All,
        NotificationLevel = null!,
        SubscribedAt = DateTime.UtcNow
    };

    private static NotificationPreferenceDecision EnabledDecision(NotificationPreferenceResolveRequest request) => new(request.CategoryCode, request.ChannelCode, true, false, false, false, "Default", null);

    private static NotificationPreferenceDecision DisabledDecision(NotificationPreferenceResolveRequest request) => new(request.CategoryCode, request.ChannelCode, false, false, false, false, "User", null);

    private static BusinessMetrics CreateMetrics()
    {
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));
        return new BusinessMetrics(meterFactory);
    }
}

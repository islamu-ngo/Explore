// ABOUTME: Unit tests for event moderation attendee notification fanout.
// ABOUTME: Verifies light context, heavy generic privacy, idempotent notification rows, and durable progress.

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

public sealed class EventModerationNotificationFanoutServiceTests
{
    private readonly IEventRegistrationIntentRepository _registrationIntentRepository = Substitute.For<IEventRegistrationIntentRepository>();
    private readonly IEventModerationRecordRepository _moderationRecordRepository = Substitute.For<IEventModerationRecordRepository>();
    private readonly INotificationRepository _notificationRepository = Substitute.For<INotificationRepository>();
    private readonly INotificationFanoutRunRepository _fanoutRunRepository = Substitute.For<INotificationFanoutRunRepository>();
    private readonly INotificationPreferenceResolver _preferenceResolver = Substitute.For<INotificationPreferenceResolver>();
    private readonly EventModerationNotificationFanoutService _service;

    public EventModerationNotificationFanoutServiceTests()
    {
        _service = new EventModerationNotificationFanoutService(
            _registrationIntentRepository,
            _moderationRecordRepository,
            _notificationRepository,
            _fanoutRunRepository,
            _preferenceResolver,
            CreateMetrics(),
            Substitute.For<ILogger<EventModerationNotificationFanoutService>>());

        _preferenceResolver.ResolveAsync(Arg.Any<NotificationPreferenceResolveRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => EnabledDecision(call.Arg<NotificationPreferenceResolveRequest>()));
    }

    [Test]
    public async Task FanoutLightModerationAsync_WithRegisteredUsers_CreatesNotificationsAndCompletesRun()
    {
        var request = CreateRequest();
        var firstUserId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var secondUserId = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var createdNotifications = new List<Notification>();
        var updatedRuns = new List<NotificationFanoutRun>();

        ConfigureNewRun(request);
        _fanoutRunRepository.Update(Arg.Do<NotificationFanoutRun>(run => updatedRuns.Add(CloneRun(run))))
            .Returns(Task.CompletedTask);
        _registrationIntentRepository.GetRegisteredUserFanoutBatchAsync(
                request.TenantId,
                request.EventId,
                Arg.Any<Guid?>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([firstUserId, secondUserId], []);
        _notificationRepository.ExistsByDeduplicationKeyAsync(
                request.TenantId,
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(false);
        _notificationRepository.Create(Arg.Do<Notification>(notification => createdNotifications.Add(notification)))
            .Returns(call => call.Arg<Notification>());

        await _service.FanoutLightModerationAsync(request);

        await Assert.That(createdNotifications).Count().IsEqualTo(2);
        await Assert.That(createdNotifications.Select(notification => notification.UserId)).Contains(firstUserId);
        await Assert.That(createdNotifications.Select(notification => notification.UserId)).Contains(secondUserId);
        var notification = createdNotifications.First();
        await Assert.That(notification.TenantId).IsEqualTo(request.TenantId);
        await Assert.That(notification.NotificationTypeId).IsEqualTo((int)NotificationTypeEnum.EventUpdated);
        await Assert.That(notification.NotificationEntityTypeId).IsEqualTo((int)NotificationEntityTypeEnum.Event);
        await Assert.That(notification.EntityId).IsEqualTo(request.EventId.ToString());
        await Assert.That(notification.NotificationScopeId).IsEqualTo((int)ActorTypeEnum.User);
        await Assert.That(notification.NotificationReasonId).IsEqualTo((int)NotificationReasonEnum.System);
        await Assert.That(notification.SourceActorId).IsEqualTo(request.SourceActorId);
        await Assert.That(notification.Title).Contains(request.EventTitle);
        await Assert.That(notification.DeduplicationKey).Contains(request.ModerationRecordId.ToString("N"));
        await Assert.That(updatedRuns.Last().Status).IsEqualTo(EventModerationNotificationFanoutService.StatusCompleted);
        await Assert.That(updatedRuns.Last().ProcessedCount).IsEqualTo(2);
        await Assert.That(updatedRuns.Last().CreatedNotificationCount).IsEqualTo(2);
        await Assert.That(updatedRuns.Last().CursorSubscriberTenantUserId).IsEqualTo(secondUserId);
    }

    [Test]
    public async Task FanoutLightModerationAsync_WithExistingDeduplicationKey_SkipsDuplicateNotification()
    {
        var request = CreateRequest();
        var userId = Guid.Parse("10000000-0000-0000-0000-000000000001");

        ConfigureNewRun(request);
        _registrationIntentRepository.GetRegisteredUserFanoutBatchAsync(
                request.TenantId,
                request.EventId,
                Arg.Any<Guid?>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([userId], []);
        _notificationRepository.ExistsByDeduplicationKeyAsync(
                request.TenantId,
                userId,
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        await _service.FanoutLightModerationAsync(request);

        await _notificationRepository.DidNotReceive().Create(Arg.Any<Notification>());
        await _fanoutRunRepository.Received().Update(Arg.Is<NotificationFanoutRun>(run =>
            run.Status == EventModerationNotificationFanoutService.StatusCompleted
            && run.ProcessedCount == 1
            && run.CreatedNotificationCount == 0));
    }

    [Test]
    public async Task FanoutHeavyRedactionAsync_WithRegisteredUsers_CreatesGenericLinklessNotificationsAndCompletesRun()
    {
        var moderationRecord = CreateHeavyModerationRecord();
        var request = CreateHeavyRequest(moderationRecord);
        var firstUserId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var secondUserId = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var createdNotifications = new List<Notification>();
        var updatedRuns = new List<NotificationFanoutRun>();

        ConfigureNewHeavyRun(request);
        _moderationRecordRepository.GetByIdAsync(
                request.TenantId,
                request.ModerationRecordId,
                Arg.Any<CancellationToken>())
            .Returns(moderationRecord);
        _fanoutRunRepository.Update(Arg.Do<NotificationFanoutRun>(run => updatedRuns.Add(CloneRun(run))))
            .Returns(Task.CompletedTask);
        _registrationIntentRepository.GetRegisteredUserFanoutBatchAsync(
                request.TenantId,
                moderationRecord.EventId,
                Arg.Any<Guid?>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([firstUserId, secondUserId], []);
        _notificationRepository.ExistsByDeduplicationKeyAsync(
                request.TenantId,
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(false);
        _notificationRepository.Create(Arg.Do<Notification>(notification => createdNotifications.Add(notification)))
            .Returns(call => call.Arg<Notification>());

        await _service.FanoutHeavyRedactionAsync(request);

        await Assert.That(createdNotifications).Count().IsEqualTo(2);
        await Assert.That(createdNotifications.Select(notification => notification.UserId)).Contains(firstUserId);
        await Assert.That(createdNotifications.Select(notification => notification.UserId)).Contains(secondUserId);
        var notification = createdNotifications.First();
        await Assert.That(notification.TenantId).IsEqualTo(request.TenantId);
        await Assert.That(notification.NotificationTypeId).IsEqualTo((int)NotificationTypeEnum.General);
        await Assert.That(notification.NotificationEntityTypeId).IsNull();
        await Assert.That(notification.EntityId).IsNull();
        await Assert.That(notification.SourceActorId).IsNull();
        await Assert.That(notification.RecipientContextActorId).IsNull();
        await Assert.That(notification.NotificationScopeId).IsEqualTo((int)ActorTypeEnum.User);
        await Assert.That(notification.NotificationReasonId).IsEqualTo((int)NotificationReasonEnum.System);
        await Assert.That(notification.Title).Contains("Event");
        await Assert.That(notification.Body).Contains("moderation");
        await Assert.That(notification.DeduplicationKey).Contains(request.ModerationRecordId.ToString("N"));
        await Assert.That(notification.DeduplicationKey).DoesNotContain(moderationRecord.EventId.ToString("N"));
        await Assert.That(notification.Title).DoesNotContain(moderationRecord.EventId.ToString());
        await Assert.That(notification.Body).DoesNotContain(moderationRecord.EventId.ToString());
        await Assert.That(notification.Title).DoesNotContain("Illegal Event");
        await Assert.That(notification.Body).DoesNotContain("Illegal Event");
        await Assert.That(notification.Title).DoesNotContain("/events/");
        await Assert.That(notification.Body).DoesNotContain("/events/");
        await Assert.That(updatedRuns.Last().Status).IsEqualTo(EventModerationNotificationFanoutService.StatusCompleted);
        await Assert.That(updatedRuns.Last().FanoutKind).IsEqualTo(EventModerationNotificationFanoutService.HeavyFanoutKind);
        await Assert.That(updatedRuns.Last().ProcessedCount).IsEqualTo(2);
        await Assert.That(updatedRuns.Last().CreatedNotificationCount).IsEqualTo(2);
        await Assert.That(updatedRuns.Last().CursorSubscriberTenantUserId).IsEqualTo(secondUserId);
    }

    [Test]
    public async Task FanoutHeavyRedactionAsync_WithExistingDeduplicationKey_SkipsDuplicateNotification()
    {
        var moderationRecord = CreateHeavyModerationRecord();
        var request = CreateHeavyRequest(moderationRecord);
        var userId = Guid.Parse("10000000-0000-0000-0000-000000000001");

        ConfigureNewHeavyRun(request);
        _moderationRecordRepository.GetByIdAsync(
                request.TenantId,
                request.ModerationRecordId,
                Arg.Any<CancellationToken>())
            .Returns(moderationRecord);
        _registrationIntentRepository.GetRegisteredUserFanoutBatchAsync(
                request.TenantId,
                moderationRecord.EventId,
                Arg.Any<Guid?>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([userId], []);
        _notificationRepository.ExistsByDeduplicationKeyAsync(
                request.TenantId,
                userId,
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        await _service.FanoutHeavyRedactionAsync(request);

        await _notificationRepository.DidNotReceive().Create(Arg.Any<Notification>());
        await _fanoutRunRepository.Received().Update(Arg.Is<NotificationFanoutRun>(run =>
            run.FanoutKind == EventModerationNotificationFanoutService.HeavyFanoutKind
            && run.Status == EventModerationNotificationFanoutService.StatusCompleted
            && run.ProcessedCount == 1
            && run.CreatedNotificationCount == 0));
    }

    [Test]
    public async Task FanoutHeavyRedactionAsync_BypassesOptionalTrustSafetyPreference()
    {
        var moderationRecord = CreateHeavyModerationRecord();
        var request = CreateHeavyRequest(moderationRecord);
        var userId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var createdNotifications = new List<Notification>();

        ConfigureNewHeavyRun(request);
        _moderationRecordRepository.GetByIdAsync(
                request.TenantId,
                request.ModerationRecordId,
                Arg.Any<CancellationToken>())
            .Returns(moderationRecord);
        _registrationIntentRepository.GetRegisteredUserFanoutBatchAsync(
                request.TenantId,
                moderationRecord.EventId,
                Arg.Any<Guid?>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([userId], []);
        _notificationRepository.ExistsByDeduplicationKeyAsync(
                request.TenantId,
                userId,
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(false);
        _notificationRepository.Create(Arg.Do<Notification>(notification => createdNotifications.Add(notification)))
            .Returns(call => call.Arg<Notification>());

        await _service.FanoutHeavyRedactionAsync(request);

        await Assert.That(createdNotifications).Count().IsEqualTo(1);
        await Assert.That(createdNotifications[0].UserId).IsEqualTo(userId);
        await _preferenceResolver.DidNotReceiveWithAnyArgs()
            .ResolveAsync(default!, default);
    }

    [Test]
    public async Task FanoutLightModerationAsync_WhenTrustSafetyPreferenceIsDisabled_SkipsNotification()
    {
        var request = CreateRequest();
        var userId = Guid.Parse("10000000-0000-0000-0000-000000000001");

        ConfigureNewRun(request);
        _registrationIntentRepository.GetRegisteredUserFanoutBatchAsync(
                request.TenantId,
                request.EventId,
                Arg.Any<Guid?>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([userId], []);
        _notificationRepository.ExistsByDeduplicationKeyAsync(
                request.TenantId,
                userId,
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(false);
        _preferenceResolver.ResolveAsync(
                Arg.Any<NotificationPreferenceResolveRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(call => DisabledDecision(call.Arg<NotificationPreferenceResolveRequest>()));

        await _service.FanoutLightModerationAsync(request);

        await _notificationRepository.DidNotReceive().Create(Arg.Any<Notification>());
    }

    [Test]
    public async Task FanoutLightModerationAsync_WithCompletedRun_DoesNothing()
    {
        var request = CreateRequest();
        _fanoutRunRepository.GetBySourceAsync(
                request.TenantId,
                EventModerationNotificationFanoutService.LightFanoutKind,
                (int)NotificationEntityTypeEnum.Event,
                request.ModerationRecordId,
                request.SourceActorId,
                true,
                Arg.Any<CancellationToken>())
            .Returns(new NotificationFanoutRun
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                Tenant = null!,
                FanoutKind = EventModerationNotificationFanoutService.LightFanoutKind,
                NotificationEntityTypeId = (int)NotificationEntityTypeEnum.Event,
                NotificationEntityType = null!,
                EntityId = request.ModerationRecordId,
                SourceActorId = request.SourceActorId,
                SourceActor = null!,
                Status = EventModerationNotificationFanoutService.StatusCompleted
            });

        await _service.FanoutLightModerationAsync(request);

        await _registrationIntentRepository.DidNotReceiveWithAnyArgs().GetRegisteredUserFanoutBatchAsync(default, default, default, default, default);
        await _notificationRepository.DidNotReceiveWithAnyArgs().Create(default!);
        await _fanoutRunRepository.DidNotReceive().Update(Arg.Any<NotificationFanoutRun>());
    }

    private void ConfigureNewRun(EventLightModeratedNotificationFanoutRequested request)
    {
        _fanoutRunRepository.GetBySourceAsync(
                request.TenantId,
                EventModerationNotificationFanoutService.LightFanoutKind,
                (int)NotificationEntityTypeEnum.Event,
                request.ModerationRecordId,
                request.SourceActorId,
                true,
                Arg.Any<CancellationToken>())
            .Returns((NotificationFanoutRun?)null);
        _fanoutRunRepository.Create(Arg.Any<NotificationFanoutRun>())
            .Returns(call => call.Arg<NotificationFanoutRun>());
    }

    private void ConfigureNewHeavyRun(EventHeavyRedactedNotificationFanoutRequested request)
    {
        _fanoutRunRepository.GetBySourceAsync(
                request.TenantId,
                EventModerationNotificationFanoutService.HeavyFanoutKind,
                (int)NotificationEntityTypeEnum.Event,
                request.ModerationRecordId,
                request.SourceActorId,
                true,
                Arg.Any<CancellationToken>())
            .Returns((NotificationFanoutRun?)null);
        _fanoutRunRepository.Create(Arg.Any<NotificationFanoutRun>())
            .Returns(call => call.Arg<NotificationFanoutRun>());
    }

    private static EventLightModeratedNotificationFanoutRequested CreateRequest() => new()
    {
        TenantId = Guid.NewGuid(),
        EventId = Guid.NewGuid(),
        ModerationRecordId = Guid.NewGuid(),
        EventTitle = "Community Iftar",
        SourceActorId = Guid.NewGuid(),
        ModeratedAt = DateTimeOffset.UtcNow
    };

    private static EventModerationRecord CreateHeavyModerationRecord()
    {
        return EventModerationRecord.CreateHeavyRedaction(
            Guid.NewGuid(),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.NewGuid(),
            "illegal_content",
            (int)EventStatusEnum.Published,
            null,
            DateTimeOffset.UtcNow);
    }

    private static EventHeavyRedactedNotificationFanoutRequested CreateHeavyRequest(
        EventModerationRecord moderationRecord) => new()
        {
            TenantId = moderationRecord.TenantId,
            ModerationRecordId = moderationRecord.Id,
            SourceActorId = Guid.NewGuid(),
            RedactedAt = moderationRecord.CreatedAt
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

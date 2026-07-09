// ABOUTME: Unit tests for delayed Event lifecycle scheduler orchestration.
// ABOUTME: Proves reminders persist EmailDispatchOutbox state before requesting scheduler wake-ups.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Notifications;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;
using NSubstitute.Core;
using TUnit.Core;

using ApplicationNotificationCategory = Explore.Application.Notifications.NotificationCategory;

namespace Event.Application.UnitTests.Services;

public sealed class EventLifecycleSchedulerTests
{
    [Test]
    public async Task ScheduleEventReminderCreatesOutboxBeforePointerTrigger()
    {
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var registrationIntentId = Guid.CreateVersion7();
        var outboxId = Guid.CreateVersion7();
        var dispatchAt = DateTimeOffset.UtcNow.AddHours(2);
        var repository = Substitute.For<IEmailDispatchOutboxRepository>();
        var trigger = Substitute.For<IScheduledEmailDispatchTrigger>();
        var notificationOrchestrator = Substitute.For<INotificationOrchestrator>();
        var scheduler = new EventLifecycleScheduler(
            new EventLifecycleEmailOutboxFactory(notificationOrchestrator),
            repository,
            trigger);

        repository.Create(Arg.Any<EmailDispatchOutbox>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var outbox = call.Arg<EmailDispatchOutbox>();
                outbox.Id = outboxId;
                return outbox;
            });
        trigger.ScheduleAsync(
                Arg.Any<ScheduledEmailDispatchPointer>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(ScheduledEmailDispatchTriggerResult.Success(Guid.CreateVersion7()));
        notificationOrchestrator.EnqueueAsync(
                Arg.Any<NotificationIntentDraft>(),
                Arg.Any<CancellationToken>())
            .Returns(CreateNotificationResult);

        var result = await scheduler.ScheduleEventReminderAsync(
            new EventReminderScheduleInput(
                tenantId,
                userId,
                eventId,
                registrationIntentId,
                "attendee@example.test",
                "Community Dinner",
                DateTimeOffset.UtcNow.AddDays(3),
                dispatchAt),
            CancellationToken.None);

        await repository.Received(1).Create(
            Arg.Is<EmailDispatchOutbox>(outbox =>
                outbox.TenantId == tenantId
                && outbox.Kind == EmailDispatchKind.EventReminder
                && outbox.Status == EmailDispatchStatus.Pending
                && outbox.NextAttemptAt == dispatchAt.UtcDateTime
                && outbox.EventId == eventId
                && outbox.RegistrationIntentId == registrationIntentId
                && outbox.UserId == userId),
            Arg.Any<CancellationToken>());
        await trigger.Received(1).ScheduleAsync(
            Arg.Is<ScheduledEmailDispatchPointer>(pointer =>
                pointer.TenantId == tenantId
                && pointer.PublishEventId == result.PublishEventId
                && pointer.UseCase == EventLifecycleAutomationUseCases.EventReminder
                && pointer.EventId == eventId
                && pointer.RegistrationIntentId == registrationIntentId
                && pointer.UserId == userId),
            dispatchAt,
            Arg.Any<CancellationToken>());
        await notificationOrchestrator.Received(1).EnqueueAsync(
            Arg.Is<NotificationIntentDraft>(draft =>
                draft.Category == ApplicationNotificationCategory.RegistrationLifecycle
                && draft.TenantId == tenantId
                && draft.RecipientKind == "User"
                && draft.TemplateKey == "event.reminder"
                && draft.SafePayloadReference == $"event-registration-intent:{registrationIntentId}"
                && draft.DeduplicationKey == $"event-registration-intent:{registrationIntentId}:event-reminder"
                && draft.CorrelationId == registrationIntentId.ToString()
                && draft.UserId == userId
                && draft.EventId == eventId),
            Arg.Any<CancellationToken>());
        await Assert.That(result.EmailDispatchOutboxId).IsEqualTo(outboxId);
        await Assert.That(result.SchedulerTriggered).IsTrue();
        await Assert.That(result.SchedulerFailureCategory).IsEqualTo("none");
    }

    [Test]
    public async Task ScheduleEventReminderStillReturnsDurableOutboxWhenTriggerIsDisabled()
    {
        var repository = Substitute.For<IEmailDispatchOutboxRepository>();
        var notificationOrchestrator = Substitute.For<INotificationOrchestrator>();
        var scheduler = new EventLifecycleScheduler(
            new EventLifecycleEmailOutboxFactory(notificationOrchestrator),
            repository,
            new NoOpScheduledEmailDispatchTrigger());
        repository.Create(Arg.Any<EmailDispatchOutbox>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var outbox = call.Arg<EmailDispatchOutbox>();
                outbox.Id = Guid.CreateVersion7();
                return outbox;
            });
        notificationOrchestrator.EnqueueAsync(
                Arg.Any<NotificationIntentDraft>(),
                Arg.Any<CancellationToken>())
            .Returns(CreateNotificationResult);

        var result = await scheduler.ScheduleEventReminderAsync(
            new EventReminderScheduleInput(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "attendee@example.test",
                "Community Dinner",
                DateTimeOffset.UtcNow.AddDays(3),
                DateTimeOffset.UtcNow.AddHours(1)),
            CancellationToken.None);

        await Assert.That(result.SchedulerTriggered).IsFalse();
        await Assert.That(result.SchedulerFailureCategory).IsEqualTo("scheduler_disabled");
        await Assert.That(result.PublishEventId).IsNotEqualTo(Guid.Empty);
        await notificationOrchestrator.Received(1).EnqueueAsync(
            Arg.Any<NotificationIntentDraft>(),
            Arg.Any<CancellationToken>());
    }

    private static NotificationOrchestrationResult CreateNotificationResult(CallInfo callInfo)
    {
        var draft = callInfo.ArgAt<NotificationIntentDraft>(0);
        return new NotificationOrchestrationResult(
            new NotificationIntent
            {
                TenantId = draft.TenantId ?? Guid.CreateVersion7(),
                Tenant = null!,
                CategoryId = (int)NotificationCategoryEnum.RegistrationLifecycle,
                Category = null!,
                OwnershipTypeId = (int)NotificationOwnershipTypeEnum.IslamuEvent,
                OwnershipType = null!,
                RecipientKindId = (int)NotificationRecipientKindEnum.User,
                RecipientKind = null!,
                StatusId = (int)NotificationIntentStatusEnum.Pending,
                Status = null!,
                TemplateKey = draft.TemplateKey ?? string.Empty,
                DeduplicationKey = draft.DeduplicationKey ?? string.Empty
            },
            new NotificationOwnershipDecision(draft.Category, NotificationOwnership.IslamuEvent));
    }
}

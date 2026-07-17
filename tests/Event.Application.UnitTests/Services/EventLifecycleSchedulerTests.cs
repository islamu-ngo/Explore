// ABOUTME: Unit tests for delayed Event lifecycle scheduler orchestration.
// ABOUTME: Proves reminder recipient state commits atomically before scheduler wake-ups.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Notifications;
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
    public async Task ScheduleEventReminderMaterializesRecipientBeforePointerTrigger()
    {
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var registrationIntentId = Guid.CreateVersion7();
        var outboxId = Guid.CreateVersion7();
        var dispatchAt = DateTimeOffset.UtcNow.AddHours(2);
        var materializer = Substitute.For<IRecipientNotificationMaterializer>();
        var trigger = Substitute.For<IScheduledEmailDispatchTrigger>();
        var notificationOrchestrator = Substitute.For<INotificationOrchestrator>();
        var calls = new List<string>();
        var scheduler = new EventLifecycleScheduler(
            new EventLifecycleEmailOutboxFactory(),
            materializer,
            trigger);

        materializer.MaterializeAsync(
                Arg.Any<RecipientNotificationMaterialization>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                calls.Add("materialize");
                var request = call.ArgAt<RecipientNotificationMaterialization>(0)!;
                request.Email!.Id = outboxId;
                return CreateMaterializationResult(request);
            });
        trigger.ScheduleAsync(
                Arg.Any<ScheduledEmailDispatchPointer>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                calls.Add("trigger");
                return ScheduledEmailDispatchTriggerResult.Success(Guid.CreateVersion7());
            });

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

        await materializer.Received(1).MaterializeAsync(
            Arg.Is<RecipientNotificationMaterialization>(request =>
                request != null
                && request.IntentId != Guid.Empty
                && request.Intent.Category == ApplicationNotificationCategory.RegistrationLifecycle
                && request.Intent.TenantId == tenantId
                && request.Intent.RecipientKind == "User"
                && request.Intent.TemplateKey == "event.reminder"
                && request.Intent.SafePayloadReference == $"event-registration-intent:{registrationIntentId}"
                && request.Intent.DeduplicationKey == $"event-registration-intent:{registrationIntentId}:event-reminder"
                && request.Intent.CorrelationId == registrationIntentId.ToString()
                && request.Intent.UserId == userId
                && request.Intent.EventId == eventId
                && request.DeliveryPolicy == NotificationDeliveryPolicyEnum.ReminderOptional
                && request.DisclosureLevel == "generic"
                && request.InApp == null
                && request.IncludeEmailChannel
                && !request.EmailRequired
                && request.PreferenceCategoryCode == NotificationPreferenceCategoryCodes.EventUpdates
                && request.EmailPreferenceEnabled == true
                && !request.LinkAllowed
                && request.Email != null
                && request.Email.TenantId == tenantId
                && request.Email.Kind == EmailDispatchKind.EventReminder
                && request.Email.Status == EmailDispatchStatus.Pending
                && request.Email.NextAttemptAt == dispatchAt.UtcDateTime
                && request.Email.EventId == eventId
                && request.Email.RegistrationIntentId == registrationIntentId
                && request.Email.RecipientUserId == userId
                && request.Email.RecipientAddressSource == RecipientAddressSource.TenantUserVerifiedEmail),
            Arg.Any<CancellationToken>());
        await trigger.Received(1).ScheduleAsync(
            Arg.Is<ScheduledEmailDispatchPointer>(pointer =>
                pointer != null
                && pointer.TenantId == tenantId
                && pointer.PublishEventId == result.PublishEventId
                && pointer.UseCase == EventLifecycleAutomationUseCases.EventReminder
                && pointer.EventId == eventId
                && pointer.RegistrationIntentId == registrationIntentId),
            dispatchAt,
            Arg.Any<CancellationToken>());
        await notificationOrchestrator.DidNotReceiveWithAnyArgs().EnqueueAsync(default!, default);
        await Assert.That(calls.Count).IsEqualTo(2);
        await Assert.That(calls[0]).IsEqualTo("materialize");
        await Assert.That(calls[1]).IsEqualTo("trigger");
        await Assert.That(result.EmailDispatchOutboxId).IsEqualTo(outboxId);
        await Assert.That(result.SchedulerTriggered).IsTrue();
        await Assert.That(result.SchedulerFailureCategory).IsEqualTo("none");
    }

    [Test]
    public async Task ScheduleEventReminderDoesNotTriggerPointerWhenAtomicMaterializationFails()
    {
        var materializer = Substitute.For<IRecipientNotificationMaterializer>();
        var trigger = Substitute.For<IScheduledEmailDispatchTrigger>();
        var scheduler = new EventLifecycleScheduler(
            new EventLifecycleEmailOutboxFactory(),
            materializer,
            trigger);
        materializer.MaterializeAsync(
                Arg.Any<RecipientNotificationMaterialization>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<RecipientNotificationMaterializationResult>(
                new InvalidOperationException("transaction rolled back")));

        await Assert.ThrowsAsync<InvalidOperationException>(() => scheduler.ScheduleEventReminderAsync(
            CreateInput(),
            CancellationToken.None));

        await trigger.DidNotReceiveWithAnyArgs().ScheduleAsync(default!, default, default);
    }

    [Test]
    public async Task ScheduleEventReminderStillReturnsDurableOutboxWhenTriggerIsDisabled()
    {
        var materializer = Substitute.For<IRecipientNotificationMaterializer>();
        var scheduler = new EventLifecycleScheduler(
            new EventLifecycleEmailOutboxFactory(),
            materializer,
            new NoOpScheduledEmailDispatchTrigger());
        materializer.MaterializeAsync(
                Arg.Any<RecipientNotificationMaterialization>(),
                Arg.Any<CancellationToken>())
            .Returns(CreateMaterializationResult);

        var result = await scheduler.ScheduleEventReminderAsync(CreateInput(), CancellationToken.None);

        await Assert.That(result.SchedulerTriggered).IsFalse();
        await Assert.That(result.SchedulerFailureCategory).IsEqualTo("scheduler_disabled");
        await Assert.That(result.PublishEventId).IsNotEqualTo(Guid.Empty);
        await materializer.Received(1).MaterializeAsync(
            Arg.Any<RecipientNotificationMaterialization>(),
            Arg.Any<CancellationToken>());
    }

    private static EventReminderScheduleInput CreateInput() => new(
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        "attendee@example.test",
        "Community Dinner",
        DateTimeOffset.UtcNow.AddDays(3),
        DateTimeOffset.UtcNow.AddHours(1));

    private static RecipientNotificationMaterializationResult CreateMaterializationResult(CallInfo callInfo) =>
        CreateMaterializationResult(callInfo.ArgAt<RecipientNotificationMaterialization>(0)!);

    private static RecipientNotificationMaterializationResult CreateMaterializationResult(
        RecipientNotificationMaterialization request)
    {
        var intent = new NotificationIntent
        {
            Id = request.IntentId,
            TenantId = request.Intent.TenantId!.Value,
            CategoryId = (int)NotificationCategoryEnum.RegistrationLifecycle,
            OwnershipTypeId = (int)NotificationOwnershipTypeEnum.IslamuEvent,
            RecipientKindId = (int)NotificationRecipientKindEnum.User,
            StatusId = (int)NotificationIntentStatusEnum.DispatchQueued,
            TemplateKey = request.Intent.TemplateKey!,
            DeduplicationKey = request.Intent.DeduplicationKey!,
            RecipientUserId = request.Intent.UserId!.Value
        };
        EmailDispatchOutbox email = request.Email!;
        email.Id = email.Id == Guid.Empty ? Guid.CreateVersion7() : email.Id;
        email.NotificationIntentId = intent.Id;
        return new RecipientNotificationMaterializationResult(intent, [], null, email);
    }
}

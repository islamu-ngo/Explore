// ABOUTME: Application scheduler for delayed Event lifecycle email automation.
// ABOUTME: Atomically materializes reminder recipient state before requesting a pointer-only wake-up.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Services;
using Explore.Application.Notifications;
using Explore.Domain;
using Explore.Domain.Enums;

namespace Explore.Application.Services;

public sealed class EventLifecycleScheduler(
    IEventLifecycleEmailOutboxFactory emailOutboxFactory,
    IRecipientNotificationMaterializer notificationMaterializer,
    IScheduledEmailDispatchTrigger scheduledEmailDispatchTrigger)
    : IEventLifecycleScheduler
{
    public async Task<EventLifecycleScheduleResult> ScheduleEventReminderAsync(
        EventReminderScheduleInput request,
        CancellationToken cancellationToken)
    {
        Validate(request);

        var outbox = emailOutboxFactory.CreateEventReminder(
            request.TenantId,
            request.UserId,
            request.EventId,
            request.RegistrationIntentId,
            request.RecipientEmail,
            request.EventTitle,
            request.EventStartsAt);

        outbox.Status = EmailDispatchStatus.Pending;
        outbox.NextAttemptAt = request.DispatchAt.UtcDateTime;
        outbox.RecipientAddressSource = RecipientAddressSource.TenantUserVerifiedEmail;

        var materialized = await notificationMaterializer.MaterializeAsync(
            CreateReminderMaterialization(request, outbox),
            cancellationToken);
        EmailDispatchOutbox persisted = materialized.Email
            ?? throw new InvalidOperationException("Reminder materialization did not return its email dispatch row.");

        var pointer = new ScheduledEmailDispatchPointer(
            persisted.TenantId,
            persisted.PublishEventId,
            EventLifecycleAutomationUseCases.EventReminder,
            persisted.EventId,
            persisted.RegistrationIntentId);

        var triggerResult = await scheduledEmailDispatchTrigger.ScheduleAsync(
            pointer,
            request.DispatchAt,
            cancellationToken);

        return new EventLifecycleScheduleResult(
            persisted.Id,
            persisted.PublishEventId,
            triggerResult.Scheduled,
            triggerResult.SchedulerJobId,
            triggerResult.FailureCategory);
    }

    private static RecipientNotificationMaterialization CreateReminderMaterialization(
        EventReminderScheduleInput request,
        EmailDispatchOutbox outbox)
    {
        Guid intentId = Guid.CreateVersion7();
        string sourceReference = $"event-registration-intent:{request.RegistrationIntentId}";
        return new RecipientNotificationMaterialization(
            intentId,
            new NotificationIntentDraft(
                Explore.Application.Notifications.NotificationCategory.RegistrationLifecycle,
                TenantId: request.TenantId,
                RecipientKind: "User",
                TemplateKey: "event.reminder",
                SafePayloadReference: sourceReference,
                IsUserFacing: true,
                IsIslamuInitiated: true,
                DeduplicationKey: $"{sourceReference}:event-reminder",
                CorrelationId: request.RegistrationIntentId.ToString(),
                UserId: request.UserId,
                EventId: request.EventId),
            NotificationDeliveryPolicyEnum.ReminderOptional,
            "generic",
            InApp: null,
            Email: outbox,
            IncludeEmailChannel: true,
            EmailRequired: false,
            PreferenceCategoryCode: NotificationPreferenceCategoryCodes.EventUpdates,
            EmailPreferenceEnabled: true);
    }

    private static void Validate(EventReminderScheduleInput request)
    {
        if (request.TenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(request));
        }

        if (request.UserId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(request));
        }

        if (request.EventId == Guid.Empty)
        {
            throw new ArgumentException("EventId is required.", nameof(request));
        }

        if (request.RegistrationIntentId == Guid.Empty)
        {
            throw new ArgumentException("RegistrationIntentId is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.RecipientEmail))
        {
            throw new ArgumentException("RecipientEmail is required.", nameof(request));
        }

        if (request.DispatchAt == default)
        {
            throw new ArgumentException("DispatchAt is required.", nameof(request));
        }
    }
}

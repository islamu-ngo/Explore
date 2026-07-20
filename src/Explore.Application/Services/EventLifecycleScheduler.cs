// ABOUTME: Materializes approved-registration reminders inside the caller-owned transaction.
// ABOUTME: Schedules pointer-only wake-ups after commit without bypassing the email eligibility fence.

using Explore.Application.Configuration;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Notifications;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Services.Scheduling;
using Microsoft.Extensions.Options;

namespace Explore.Application.Services;

public sealed class EventLifecycleScheduler(
    IEventLifecycleEmailOutboxFactory emailOutboxFactory,
    IRecipientNotificationMaterializer notificationMaterializer,
    IEventRegistrationIntentRepository registrationIntentRepository,
    INotificationFanoutOccurrenceRepository fanoutOccurrenceRepository,
    IEmailDispatchOutboxRepository emailDispatchOutboxRepository,
    INotificationPreferenceResolver preferenceResolver,
    IScheduledEmailDispatchTrigger scheduledEmailDispatchTrigger,
    IOptions<EventReminderOptions> reminderOptions)
    : IEventLifecycleScheduler
{
    public async Task<EventReminderPreparedSchedule?> PrepareEventReminderInCurrentTransactionAsync(
        EventReminderPreparationInput request,
        CancellationToken cancellationToken)
    {
        Validate(request);
        if (!IsNewlyApproved(request))
        {
            return null;
        }

        if (await fanoutOccurrenceRepository.AcquireEventPrecedenceLockAndHasHeavyAuthorityAsync(
            request.RegistrationIntent.TenantId,
            request.RegistrationIntent.EventId,
            cancellationToken))
        {
            return null;
        }

        EventSession? session = await registrationIntentRepository.GetEarliestApprovedReminderSessionAsync(
            request.RegistrationIntent.TenantId,
            request.RegistrationIntent.Id,
            request.SchedulingReferenceAt,
            cancellationToken);
        if (session?.StartTime is null)
        {
            return null;
        }

        return await MaterializeReminderAsync(
            request.RegistrationIntent,
            request.Recipient,
            request.EventTitle,
            session,
            request.SchedulingReferenceAt,
            request.GraphIds,
            request.Transition.OccurrenceId.ToString("D"),
            request.EventTimeZoneId,
            cancellationToken);
    }

    public async Task SuppressEventRemindersInCurrentTransactionAsync(
        EventReminderSuppressionInput request,
        CancellationToken cancellationToken)
    {
        Validate(request);
        await fanoutOccurrenceRepository.AcquireEventPrecedenceLockAndHasHeavyAuthorityAsync(
            request.TenantId,
            request.EventId,
            cancellationToken);
        await emailDispatchOutboxRepository.SuppressEventRemindersInCurrentTransactionAsync(
            new EventReminderSupersessionRequest(
                request.TenantId,
                request.EventId,
                request.RegistrationIntentId,
                request.SessionId,
                request.SuppressedAt,
                request.ReasonCode),
            cancellationToken);
    }

    public async Task ReprojectEventRemindersInCurrentTransactionAsync(
        EventReminderReprojectionInput request,
        CancellationToken cancellationToken)
    {
        Validate(request);
        bool hasHeavyAuthority = await fanoutOccurrenceRepository.AcquireEventPrecedenceLockAndHasHeavyAuthorityAsync(
            request.TenantId,
            request.EventId,
            cancellationToken);
        if (hasHeavyAuthority)
        {
            await emailDispatchOutboxRepository.SuppressEventRemindersInCurrentTransactionAsync(
                new EventReminderSupersessionRequest(
                    request.TenantId,
                    request.EventId,
                    RegistrationIntentId: null,
                    request.SessionId,
                    request.ChangedAt.UtcDateTime,
                    "event_reminder_heavy_authority"),
                cancellationToken);
            return;
        }

        await emailDispatchOutboxRepository.RescheduleEventRemindersInCurrentTransactionAsync(
            new EventReminderRescheduleRequest(
                request.TenantId,
                request.EventId,
                request.RegistrationIntentId,
                request.SessionId,
                request.EventTitle,
                reminderOptions.Value.GetValidatedLeadTime(),
                request.ChangedAt.UtcDateTime,
                request.EventTimeZoneId),
            cancellationToken);
    }

    private async Task<EventReminderPreparedSchedule?> MaterializeReminderAsync(
        EventRegistrationIntent registrationIntent,
        User recipient,
        string eventTitle,
        EventSession session,
        DateTimeOffset schedulingReferenceAt,
        EventReminderGraphIds graphIds,
        string correlationId,
        string eventTimeZoneId,
        CancellationToken cancellationToken)
    {
        string normalizedTimeZoneId = ScheduleTimeZoneResolver.NormalizeOrUtc(eventTimeZoneId);
        DateTimeOffset sessionStartUtc = session.StartTime!.Value.ToUniversalTime();
        if (sessionStartUtc <= schedulingReferenceAt.ToUniversalTime())
        {
            return null;
        }

        TimeSpan leadTime = reminderOptions.Value.GetValidatedLeadTime();
        DateTimeOffset calculatedDueAt = sessionStartUtc - leadTime;
        DateTimeOffset dispatchAt = calculatedDueAt > schedulingReferenceAt
            ? calculatedDueAt
            : schedulingReferenceAt;

        NotificationPreferenceDecision[] preferences = (await preferenceResolver.ResolveBatchAsync(
            [
                new NotificationPreferenceResolveRequest(
                    registrationIntent.TenantId,
                    registrationIntent.UserId,
                    OrganizationId: null,
                    GroupId: null,
                    NotificationPreferenceCategoryCodes.EventUpdates,
                    NotificationPreferenceChannelCodes.InApp),
                new NotificationPreferenceResolveRequest(
                    registrationIntent.TenantId,
                    registrationIntent.UserId,
                    OrganizationId: null,
                    GroupId: null,
                    NotificationPreferenceCategoryCodes.EventUpdates,
                    NotificationPreferenceChannelCodes.Email)
            ],
            cancellationToken)).ToArray();
        bool inAppPreferenceEnabled = preferences.Single(value =>
            value.ChannelCode == NotificationPreferenceChannelCodes.InApp).IsEnabled;
        bool emailPreferenceEnabled = preferences.Single(value =>
            value.ChannelCode == NotificationPreferenceChannelCodes.Email).IsEnabled;

        RecipientEmailAddressResolution address = emailPreferenceEnabled
            ? RecipientEmailAddressResolver.Resolve(
                recipient,
                registrationIntent.UserId)
            : new RecipientEmailAddressResolution(null, "email_preference_disabled");
        EmailDispatchOutbox? email = emailPreferenceEnabled && address.HasVerifiedEmail
            ? emailOutboxFactory.CreateEventReminder(
                registrationIntent.TenantId,
                registrationIntent.UserId,
                registrationIntent.EventId,
                registrationIntent.Id,
                address.Email!,
                eventTitle,
                sessionStartUtc,
                normalizedTimeZoneId)
            : null;
        if (email is not null)
        {
            email.Id = graphIds.EmailDispatchOutboxId;
            email.PublishEventId = graphIds.PublishEventId;
            email.Status = EmailDispatchStatus.Pending;
            email.NextAttemptAt = dispatchAt.UtcDateTime;
            email.RecipientAddressSource = RecipientAddressSource.TenantUserVerifiedEmail;
            email.CorrelationId = EventReminderAuthorityReference.Format(
                session.Id,
                sessionStartUtc,
                normalizedTimeZoneId);
        }

        string sourceReference =
            $"event-registration-intent:{registrationIntent.Id:N}:session:{session.Id:N}";
        string deduplicationKey = $"{sourceReference}:event-reminder";
        RecipientNotificationMaterializationResult materialized =
            await notificationMaterializer.MaterializeInCurrentTransactionAsync(
                new RecipientNotificationMaterialization(
                    graphIds.NotificationIntentId,
                    new NotificationIntentDraft(
                        Explore.Application.Notifications.NotificationCategory.RegistrationLifecycle,
                        TenantId: registrationIntent.TenantId,
                        RecipientKind: "User",
                        TemplateKey: "event.reminder",
                        SafePayloadReference: sourceReference,
                        IsUserFacing: true,
                        IsIslamuInitiated: true,
                        DeduplicationKey: deduplicationKey,
                        CorrelationId: correlationId,
                        UserId: registrationIntent.UserId,
                        EventId: registrationIntent.EventId),
                    NotificationDeliveryPolicyEnum.ReminderOptional,
                    "reminder",
                    inAppPreferenceEnabled
                        ? new RecipientInAppNotificationDraft(
                            (int)NotificationTypeEnum.General,
                            $"Reminder: {NormalizeTitle(eventTitle)}",
                            $"{NormalizeTitle(eventTitle)} starts at {EventReminderAuthorityReference.FormatDisplay(sessionStartUtc, normalizedTimeZoneId)}.",
                            (int)ActorTypeEnum.User,
                            (int)NotificationReasonEnum.System,
                            (int)NotificationEntityTypeEnum.EventSession,
                            session.Id.ToString("D"),
                            IsRequired: false)
                        : null,
                    email,
                    IncludeEmailChannel: true,
                    EmailRequired: false,
                    EmailSkipReason: address.SkipReason,
                    PreferenceCategoryCode: NotificationPreferenceCategoryCodes.EventUpdates,
                    EmailPreferenceEnabled: emailPreferenceEnabled,
                    LinkAllowed: false,
                    InAppNotificationId: graphIds.InAppNotificationId,
                    InAppDeliveryId: graphIds.InAppDeliveryId,
                    EmailDeliveryId: graphIds.EmailDeliveryId,
                    MaterializedAt: schedulingReferenceAt.UtcDateTime,
                    IncludeInAppChannel: true,
                    InAppPreferenceEnabled: inAppPreferenceEnabled,
                    InAppSkipReason: inAppPreferenceEnabled ? null : "in_app_preference_disabled"),
                cancellationToken);

        EmailDispatchOutbox? persisted = materialized.Email;
        return persisted is null
            ? null
            : new EventReminderPreparedSchedule(
                persisted.TenantId,
                persisted.Id,
                persisted.PublishEventId,
                registrationIntent.EventId,
                registrationIntent.Id,
                session.Id,
                sessionStartUtc,
                dispatchAt.ToUniversalTime());
    }

    public async Task<EventLifecycleScheduleResult> TriggerPreparedEventReminderAsync(
        EventReminderPreparedSchedule request,
        CancellationToken cancellationToken)
    {
        Validate(request);
        var pointer = new ScheduledEmailDispatchPointer(
            request.TenantId,
            request.PublishEventId,
            EventLifecycleAutomationUseCases.EventReminder,
            request.EventId,
            request.RegistrationIntentId);
        DateTimeOffset dueAt = request.DispatchAt > DateTimeOffset.UtcNow
            ? request.DispatchAt
            : DateTimeOffset.UtcNow;
        ScheduledEmailDispatchTriggerResult triggerResult = await scheduledEmailDispatchTrigger.ScheduleAsync(
            pointer,
            dueAt,
            cancellationToken);

        return new EventLifecycleScheduleResult(
            request.EmailDispatchOutboxId,
            request.PublishEventId,
            triggerResult.Scheduled,
            triggerResult.SchedulerJobId,
            triggerResult.FailureCategory);
    }

    private static bool IsNewlyApproved(EventReminderPreparationInput request) =>
        request.Transition.Changed
        && request.Transition.ParentIntentId == request.RegistrationIntent.Id
        && request.Transition.FinalStatus == (int)ApprovalStatusEnum.Approved
        && request.Transition.ChildTransitions.Any(child =>
            child.PreviousStatus != (int)ApprovalStatusEnum.Approved
            && child.FinalStatus == (int)ApprovalStatusEnum.Approved
            && child.EventSessionId != Guid.Empty);

    private static void Validate(EventReminderPreparationInput request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.RegistrationIntent.Id == Guid.Empty
            || request.RegistrationIntent.TenantId == Guid.Empty
            || request.RegistrationIntent.EventId == Guid.Empty
            || request.RegistrationIntent.UserId == Guid.Empty
            || request.Recipient.Id != request.RegistrationIntent.UserId)
        {
            throw new ArgumentException(
                "Reminder preparation requires matching registration, tenant, event, and recipient authority.",
                nameof(request));
        }

        if (request.SchedulingReferenceAt == default)
        {
            throw new ArgumentException("SchedulingReferenceAt is required.", nameof(request));
        }

        _ = ScheduleTimeZoneResolver.ResolveRequired(request.EventTimeZoneId);

        EventReminderGraphIds ids = request.GraphIds;
        if (ids.NotificationIntentId == Guid.Empty
            || ids.InAppNotificationId == Guid.Empty
            || ids.InAppDeliveryId == Guid.Empty
            || ids.EmailDeliveryId == Guid.Empty
            || ids.EmailDispatchOutboxId == Guid.Empty
            || ids.PublishEventId == Guid.Empty)
        {
            throw new ArgumentException("Pre-generated reminder graph identifiers are required.", nameof(request));
        }
    }

    private static void Validate(EventReminderPreparedSchedule request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.TenantId == Guid.Empty
            || request.EmailDispatchOutboxId == Guid.Empty
            || request.PublishEventId == Guid.Empty
            || request.EventId == Guid.Empty
            || request.RegistrationIntentId == Guid.Empty
            || request.SessionId == Guid.Empty
            || request.DispatchAt == default)
        {
            throw new ArgumentException("A complete persisted reminder pointer is required.", nameof(request));
        }
    }

    private static void Validate(EventReminderSuppressionInput request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.TenantId == Guid.Empty
            || request.EventId == Guid.Empty
            || request.SuppressedAt.Kind != DateTimeKind.Utc
            || string.IsNullOrWhiteSpace(request.ReasonCode))
        {
            throw new ArgumentException("Reminder suppression requires exact event authority, a UTC time, and a reason.", nameof(request));
        }
    }

    private static void Validate(EventReminderReprojectionInput request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.TenantId == Guid.Empty
            || request.EventId == Guid.Empty
            || request.ChangedAt == default)
        {
            throw new ArgumentException("Reminder reprojection requires exact event authority and a change time.", nameof(request));
        }

        _ = ScheduleTimeZoneResolver.ResolveRequired(request.EventTimeZoneId);
    }

    private static string NormalizeTitle(string eventTitle) =>
        string.IsNullOrWhiteSpace(eventTitle) ? "the event" : eventTitle.Trim();

}

// ABOUTME: Application boundary for transaction-bound reminder creation and post-commit wake-up scheduling.
// ABOUTME: Keeps PostgreSQL recipient delivery authoritative while TickerQ carries pointer-only acceleration.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;

namespace Explore.Application.Contracts.Services;

public interface IEventLifecycleScheduler
{
    Task<EventReminderPreparedSchedule?> PrepareEventReminderInCurrentTransactionAsync(
        EventReminderPreparationInput request,
        CancellationToken cancellationToken);

    Task<EventLifecycleScheduleResult> TriggerPreparedEventReminderAsync(
        EventReminderPreparedSchedule request,
        CancellationToken cancellationToken);

    Task SuppressEventRemindersInCurrentTransactionAsync(
        EventReminderSuppressionInput request,
        CancellationToken cancellationToken);

    Task ReprojectEventRemindersInCurrentTransactionAsync(
        EventReminderReprojectionInput request,
        CancellationToken cancellationToken);
}

public sealed record EventReminderPreparationInput(
    EventRegistrationIntent RegistrationIntent,
    EventRegistrationTransitionResult Transition,
    User Recipient,
    string EventTitle,
    DateTimeOffset SchedulingReferenceAt,
    EventReminderGraphIds GraphIds,
    string EventTimeZoneId = "UTC");

public sealed record EventReminderGraphIds(
    Guid NotificationIntentId,
    Guid InAppNotificationId,
    Guid InAppDeliveryId,
    Guid EmailDeliveryId,
    Guid EmailDispatchOutboxId,
    Guid PublishEventId)
{
    public static EventReminderGraphIds Create() => new(
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        Guid.CreateVersion7());

}

public sealed record EventReminderSuppressionInput(
    Guid TenantId,
    Guid EventId,
    Guid? RegistrationIntentId,
    Guid? SessionId,
    DateTime SuppressedAt,
    string ReasonCode);

public sealed record EventReminderReprojectionInput(
    Guid TenantId,
    Guid EventId,
    Guid? RegistrationIntentId,
    Guid? SessionId,
    string EventTitle,
    DateTimeOffset ChangedAt,
    string EventTimeZoneId = "UTC");

public sealed record EventReminderPreparedSchedule(
    Guid TenantId,
    Guid EmailDispatchOutboxId,
    Guid PublishEventId,
    Guid EventId,
    Guid RegistrationIntentId,
    Guid SessionId,
    DateTimeOffset SessionStartUtc,
    DateTimeOffset DispatchAt);

public sealed record EventLifecycleScheduleResult(
    Guid EmailDispatchOutboxId,
    Guid PublishEventId,
    bool SchedulerTriggered,
    Guid? SchedulerJobId,
    string SchedulerFailureCategory);

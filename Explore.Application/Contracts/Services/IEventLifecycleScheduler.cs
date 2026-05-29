// ABOUTME: Application service contract for delayed Event lifecycle automation.
// ABOUTME: Persists durable EmailDispatchOutbox state before requesting scheduler wake-ups.

namespace Explore.Application.Contracts.Services;

public interface IEventLifecycleScheduler
{
    Task<EventLifecycleScheduleResult> ScheduleEventReminderAsync(
        EventReminderScheduleInput request,
        CancellationToken cancellationToken);
}

public sealed record EventReminderScheduleInput(
    Guid TenantId,
    Guid UserId,
    Guid EventId,
    Guid RegistrationIntentId,
    string RecipientEmail,
    string EventTitle,
    DateTimeOffset EventStartsAt,
    DateTimeOffset DispatchAt);

public sealed record EventLifecycleScheduleResult(
    Guid EmailDispatchOutboxId,
    Guid PublishEventId,
    bool SchedulerTriggered,
    Guid? SchedulerJobId,
    string SchedulerFailureCategory);

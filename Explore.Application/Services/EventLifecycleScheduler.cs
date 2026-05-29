// ABOUTME: Application scheduler for delayed Event lifecycle email automation.
// ABOUTME: Writes EmailDispatchOutbox first, then requests a pointer-only scheduler wake-up.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Domain;

namespace Explore.Application.Services;

public sealed class EventLifecycleScheduler(
    IEventLifecycleEmailOutboxFactory emailOutboxFactory,
    IEmailDispatchOutboxRepository emailDispatchOutboxRepository,
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

        var persisted = await emailDispatchOutboxRepository.Create(outbox, cancellationToken);
        var pointer = new ScheduledEmailDispatchPointer(
            persisted.TenantId,
            persisted.PublishEventId,
            EventLifecycleAutomationUseCases.EventReminder,
            persisted.EventId,
            persisted.RegistrationIntentId,
            persisted.UserId);

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

// ABOUTME: Quartz-backed scheduler trigger that wakes persisted EmailDispatchOutbox work at a due time.
// ABOUTME: Stores only durable identifiers in scheduler state and leaves dispatch truth in the application database.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Scheduling;
using Quartz;

namespace Explore.API.Scheduling;

/// <summary>
/// Schedules a single one-off trigger per pointer. Delivery retries are deliberately *not* modelled here:
/// the recurring email-dispatch drain remains the single retry authority, so a failed wake-up simply leaves
/// the outbox row due and the next drain pass picks it up.
/// </summary>
public sealed class QuartzScheduledEmailDispatchTrigger(
    ISchedulerFactory schedulerFactory,
    ILogger<QuartzScheduledEmailDispatchTrigger> logger)
    : IScheduledEmailDispatchTrigger
{
    public async Task<ScheduledEmailDispatchTriggerResult> ScheduleAsync(
        ScheduledEmailDispatchPointer pointer,
        DateTimeOffset dueAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pointer);

        var triggerId = Guid.CreateVersion7();

        try
        {
            var scheduler = await schedulerFactory.GetScheduler(cancellationToken);

            var trigger = TriggerBuilder.Create()
                .WithIdentity(triggerId.ToString("N"), QuartzSchedulerKeys.OnDemandGroup)
                .ForJob(QuartzSchedulerKeys.EventReminderDispatch)
                .StartAt(dueAt)
                .WithSimpleSchedule(schedule => schedule
                    .WithMisfireHandlingInstructionFireNow())
                .UsingJobData(
                    QuartzSchedulerKeys.DispatchPointerDataKey,
                    JsonSerializer.Serialize(pointer))
                .Build();

            await scheduler.ScheduleJob(trigger, cancellationToken);
            return ScheduledEmailDispatchTriggerResult.Success(triggerId);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(
                "Quartz scheduled dispatch trigger failed for job {JobName}. FailureType={FailureType}",
                ScheduledJobNames.EventReminderDispatch,
                exception.GetType().Name);
            return ScheduledEmailDispatchTriggerResult.NotScheduled("scheduler_unavailable");
        }
    }
}

// ABOUTME: Quartz job that wakes one pre-persisted event reminder EmailDispatchOutbox row at its due time.
// ABOUTME: Resolves pointer-only scheduler payloads back into the durable EmailDispatchOutbox drain service.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Scheduling;
using Explore.Application.Contracts.Services;
using Explore.Application.Services;
using Quartz;

namespace Explore.API.Scheduling;

/// <summary>
/// The trigger carries only durable identifiers as JSON in the <see cref="JobDataMap"/>; message content and
/// transport data stay in the application database, so a stale scheduler row can never resend real content.
/// </summary>
[DisallowConcurrentExecution]
public sealed class EventReminderDispatchJob(
    IEmailDispatchDrainService drainService,
    ILogger<EventReminderDispatchJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var pointer = ReadPointer(context);
        if (pointer is null)
        {
            logger.LogWarning(
                "Quartz job {JobName} skipped because no usable pointer was supplied.",
                ScheduledJobNames.EventReminderDispatch);
            return;
        }

        if (!string.Equals(pointer.UseCase, EventLifecycleAutomationUseCases.EventReminder, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Quartz job {JobName} skipped unsupported use case {UseCase}.",
                ScheduledJobNames.EventReminderDispatch,
                pointer.UseCase);
            return;
        }

        var result = await drainService.ProcessSingleAsync(
            pointer.TenantId,
            pointer.PublishEventId,
            ScheduledJobNames.EventReminderDispatch,
            context.CancellationToken);

        logger.LogInformation(
            "Quartz job {JobName} completed with outcome {Outcome}.",
            ScheduledJobNames.EventReminderDispatch,
            result.Outcome);
    }

    /// <summary>
    /// Reads from the merged map so a payload supplied on either the job detail or the trigger is honored.
    /// A malformed payload is a poison message: it is logged and dropped rather than retried forever.
    /// </summary>
    private ScheduledEmailDispatchPointer? ReadPointer(IJobExecutionContext context)
    {
        // GetString throws when the key is absent, so probe first: a trigger with no payload is a
        // recoverable no-op, not an exception the scheduler should retry.
        if (!context.MergedJobDataMap.TryGetValue(QuartzSchedulerKeys.DispatchPointerDataKey, out var rawPayload) ||
            rawPayload is not string payload ||
            string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ScheduledEmailDispatchPointer>(payload);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "Quartz job {JobName} could not deserialize its scheduled dispatch pointer.",
                ScheduledJobNames.EventReminderDispatch);
            return null;
        }
    }
}

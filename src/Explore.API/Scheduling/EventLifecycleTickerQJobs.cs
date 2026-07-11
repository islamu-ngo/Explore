// ABOUTME: TickerQ job functions for delayed Event lifecycle dispatch triggers.
// ABOUTME: Resolves pointer-only scheduler requests back into the durable EmailDispatchOutbox drain service.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Scheduling;
using Explore.Application.Contracts.Services;
using Explore.Application.Services;
using TickerQ.Utilities.Base;

namespace Explore.API.Scheduling;

public sealed class EventLifecycleTickerQJobs(
    IEmailDispatchDrainService drainService,
    ILogger<EventLifecycleTickerQJobs> logger)
{
    [TickerFunction(ScheduledJobNames.EventReminderDispatch)]
    public async Task DispatchEventReminderAsync(
        TickerFunctionContext<ScheduledEmailDispatchPointer>? context,
        CancellationToken cancellationToken)
    {
        var pointer = context?.Request;
        if (pointer is null)
        {
            logger.LogWarning(
                "TickerQ job {JobName} skipped because no pointer was supplied.",
                ScheduledJobNames.EventReminderDispatch);
            return;
        }

        if (!string.Equals(pointer.UseCase, EventLifecycleAutomationUseCases.EventReminder, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "TickerQ job {JobName} skipped unsupported use case {UseCase}.",
                ScheduledJobNames.EventReminderDispatch,
                pointer.UseCase);
            return;
        }

        var result = await drainService.ProcessSingleAsync(
            pointer.TenantId,
            pointer.PublishEventId,
            ScheduledJobNames.EventReminderDispatch,
            cancellationToken);

        logger.LogInformation(
            "TickerQ job {JobName} completed with outcome {Outcome}.",
            ScheduledJobNames.EventReminderDispatch,
            result.Outcome);
    }
}

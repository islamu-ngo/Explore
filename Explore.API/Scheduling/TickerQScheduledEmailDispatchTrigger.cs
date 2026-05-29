// ABOUTME: TickerQ-backed scheduler trigger for persisted EmailDispatchOutbox work.
// ABOUTME: Stores only durable identifiers in scheduler state and leaves dispatch truth in PostgreSQL.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Scheduling;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

namespace Explore.API.Scheduling;

public sealed class TickerQScheduledEmailDispatchTrigger(
    ITimeTickerManager<TimeTickerEntity> timeTickerManager,
    ILogger<TickerQScheduledEmailDispatchTrigger> logger)
    : IScheduledEmailDispatchTrigger
{
    private static readonly int[] RetryIntervals = [10, 60, 300];

    public async Task<ScheduledEmailDispatchTriggerResult> ScheduleAsync(
        ScheduledEmailDispatchPointer pointer,
        DateTimeOffset dueAt,
        CancellationToken cancellationToken)
    {
        var ticker = new TimeTickerEntity
        {
            Function = ScheduledJobNames.EventReminderDispatch,
            ExecutionTime = dueAt.UtcDateTime,
            Request = JsonSerializer.SerializeToUtf8Bytes(pointer),
            Retries = RetryIntervals.Length,
            RetryIntervals = RetryIntervals
        };

        try
        {
            var result = await timeTickerManager.AddAsync(ticker, cancellationToken);
            if (result.IsSucceeded)
            {
                return ScheduledEmailDispatchTriggerResult.Success(result.Result.Id);
            }

            logger.LogWarning(
                "TickerQ did not accept scheduled dispatch trigger for job {JobName}.",
                ScheduledJobNames.EventReminderDispatch);
            return ScheduledEmailDispatchTriggerResult.NotScheduled("tickerq_add_failed");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(
                "TickerQ scheduled dispatch trigger failed for job {JobName}. FailureType={FailureType}",
                ScheduledJobNames.EventReminderDispatch,
                exception.GetType().Name);
            return ScheduledEmailDispatchTriggerResult.NotScheduled("tickerq_unavailable");
        }
    }
}

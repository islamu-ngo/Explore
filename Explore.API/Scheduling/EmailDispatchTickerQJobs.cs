// ABOUTME: TickerQ job functions for scheduling durable email dispatch drains.
// ABOUTME: Delegates execution to Application contracts so TickerQ never owns email delivery state.

using Explore.Application.Contracts.Services;
using TickerQ.Utilities.Base;

namespace Explore.API.Scheduling;

public sealed class EmailDispatchTickerQJobs(
    IEmailDispatchDrainService drainService,
    ILogger<EmailDispatchTickerQJobs> logger)
{
    public const string EmailDispatchDrainJobName = "email-dispatch-drain";

    [TickerFunction(EmailDispatchDrainJobName, "*/10 * * * * *")]
    public async Task DrainEmailDispatchOutboxAsync(TickerFunctionContext context, CancellationToken cancellationToken)
    {
        context.CronOccurrenceOperations?.SkipIfAlreadyRunning();

        var result = await drainService.ProcessBatchAsync(cancellationToken);
        logger.LogInformation(
            "TickerQ job {JobName} completed. Pending={PendingCount}, Processed={ProcessedCount}, Sent={SentCount}, RetryScheduled={RetryScheduledCount}, DeadLettered={DeadLetteredCount}, Unknown={UnknownCount}, TenantPaused={TenantPausedCount}, AlreadyClaimed={AlreadyClaimedCount}",
            EmailDispatchDrainJobName,
            result.PendingCount,
            result.ProcessedCount,
            result.SentCount,
            result.RetryScheduledCount,
            result.DeadLetteredCount,
            result.UnknownCount,
            result.TenantPausedCount,
            result.AlreadyClaimedCount);
    }
}

// ABOUTME: Runs one bounded queued webhook bulk-replay pass under Quartz.
// ABOUTME: Leaves reservation limits, atomic scheduling, tenant isolation, and audit in Infrastructure.

using Explore.Application.Contracts.Scheduling;
using Explore.Application.Contracts.Webhooks;
using Quartz;

namespace Explore.API.Scheduling;

[DisallowConcurrentExecution]
public sealed class WebhookBulkReplayDrainJob(
    IWebhookBulkReplayService service,
    ILogger<WebhookBulkReplayDrainJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        WebhookBulkReplayProcessResult result = await service.ProcessQueuedAsync(context.CancellationToken);
        logger.LogInformation(
            "Scheduled job {JobName} completed. Completed={CompletedCount}, Scheduled={ScheduledCount}, Failed={FailedCount}",
            ScheduledJobNames.WebhookBulkReplayDrain,
            result.CompletedOperations,
            result.ScheduledTargets,
            result.FailedOperations);
    }
}

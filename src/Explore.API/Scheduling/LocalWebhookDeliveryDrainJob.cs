// ABOUTME: Runs one bounded Local-provider webhook delivery and stale-lease recovery pass under Quartz.
// ABOUTME: Delegates HTTP delivery, tenant fairness, retry, and exact-fence settlement to Infrastructure.

using Explore.Application.Contracts.Scheduling;
using Explore.Application.Contracts.Services;
using Quartz;

namespace Explore.API.Scheduling;

[DisallowConcurrentExecution]
public sealed class LocalWebhookDeliveryDrainJob(
    IWebhookDeliveryDrainService service,
    ILogger<LocalWebhookDeliveryDrainJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        WebhookDeliveryRecoveryResult recovery = await service.RecoverStaleProcessingAsync(context.CancellationToken);
        WebhookDeliveryDrainResult result = await service.ProcessBatchAsync(context.CancellationToken);
        logger.LogInformation(
            "Scheduled job {JobName} completed. Processed={ProcessedCount}, Recovered={RecoveredCount}",
            ScheduledJobNames.LocalWebhookDeliveryDrain,
            result.ProcessedCount,
            recovery.RecoveredCount);
    }
}

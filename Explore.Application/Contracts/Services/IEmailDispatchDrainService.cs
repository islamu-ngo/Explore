// ABOUTME: Application boundary for draining durable EmailDispatchOutbox work.
// ABOUTME: Lets hosted services and future schedulers trigger dispatch without owning email state transitions.

namespace Explore.Application.Contracts.Services;

public interface IEmailDispatchDrainService
{
    Task<EmailDispatchDrainResult> ProcessBatchAsync(CancellationToken cancellationToken);
}

public sealed record EmailDispatchDrainResult(
    int PendingCount,
    int ProcessedCount,
    int SentCount,
    int RetryScheduledCount,
    int DeadLetteredCount,
    int UnknownCount,
    int TenantPausedCount,
    int AlreadyClaimedCount);

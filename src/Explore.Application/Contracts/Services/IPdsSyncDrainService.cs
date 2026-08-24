// ABOUTME: Scheduler-neutral contract for one bounded AT Protocol PDS outbox drain pass.
// ABOUTME: Reports only aggregate outcomes while durable leases and provider details remain internal.

namespace Explore.Application.Contracts.Services;

public interface IPdsSyncDrainService
{
    Task<PdsSyncDrainResult> ProcessBatchAsync(CancellationToken cancellationToken);
}

public sealed record PdsSyncDrainResult(
    int ClaimedCount,
    int DeliveredCount,
    int FailedCount,
    int ClaimLostCount);

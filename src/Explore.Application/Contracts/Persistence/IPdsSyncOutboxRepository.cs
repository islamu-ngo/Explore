// ABOUTME: Repository contract for immutable PDS delivery intent and token-bearing fenced state transitions.
// ABOUTME: Excludes every unfenced legacy mutation so stale workers cannot settle or overwrite reclaimed claims.

using Explore.Domain.Federation;

namespace Explore.Application.Contracts.Persistence;

public sealed record PdsSyncClaim(
    Guid OutboxId,
    Guid TenantId,
    Guid UserId,
    Guid LeaseToken,
    long LeaseFence);

public interface IPdsSyncOutboxRepository
{
    Task AddAsync(PdsSyncOutbox outbox, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PdsSyncClaim>> ClaimDueAsync(
        int batchSize,
        string leaseOwner,
        DateTime claimedAt,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    Task<PdsSyncOutbox?> GetActiveClaimAsync(
        PdsSyncClaim claim,
        DateTime observedAt,
        CancellationToken cancellationToken = default);

    Task<bool> TryRenewClaimAsync(
        PdsSyncClaim claim,
        DateTime observedAt,
        DateTime leaseExpiresAt,
        CancellationToken cancellationToken = default);

    Task<bool> TrySettleAsync(
        PdsSyncClaim claim,
        string? uri,
        string? cid,
        DateTime settledAt,
        CancellationToken cancellationToken = default);

    Task<bool> TryFailAsync(
        PdsSyncClaim claim,
        string failureCode,
        bool retryable,
        DateTime failedAt,
        TimeSpan retryDelay,
        CancellationToken cancellationToken = default);

    Task<int> SupersedePriorAsync(
        Guid tenantId,
        string sourceEntityType,
        Guid sourceEntityId,
        Guid supersedingOutboxId,
        DateTime supersededAt,
        CancellationToken cancellationToken = default);
}

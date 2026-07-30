// ABOUTME: Repository interface for idempotency replay persistence and atomic claims.
// ABOUTME: Scopes lookup, claim, completion, and release operations by durable tenant-bound records.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public sealed record IdempotencyClaim(IdempotencyRecord Record, bool IsOwner);

public interface IIdempotencyRepository
{
    Task<IdempotencyRecord?> FindAsync(string key, Guid tenantId, CancellationToken cancellationToken = default);
    Task SaveAsync(IdempotencyRecord record, CancellationToken cancellationToken = default);
    Task<IdempotencyClaim> TryClaimAsync(IdempotencyRecord record, CancellationToken cancellationToken = default);
    Task<bool> CompleteAsync(
        Guid recordId,
        int statusCode,
        string? responseBody,
        string? contentType,
        CancellationToken cancellationToken = default);
    Task<bool> ReleaseAsync(Guid recordId, CancellationToken cancellationToken = default);
    Task<int> CountExpiredAsync(DateTime expiresBeforeUtc, int batchSize, CancellationToken cancellationToken = default);
    Task<int> DeleteExpiredAsync(DateTime expiresBeforeUtc, int batchSize, CancellationToken cancellationToken = default);
}

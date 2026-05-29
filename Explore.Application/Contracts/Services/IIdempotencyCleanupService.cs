// ABOUTME: Application contract for deleting expired idempotency replay-cache records.
// ABOUTME: Keeps cleanup orchestration independent from API hosted-service scheduling mechanics.

using Explore.Application.Models;

namespace Explore.Application.Contracts.Services;

public interface IIdempotencyCleanupService
{
    Task<IdempotencyCleanupResult> CleanupExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}

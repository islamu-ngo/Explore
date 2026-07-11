// ABOUTME: Result model for one expired idempotency-record cleanup pass.
// ABOUTME: Reports the cutoff, eligible row count, deleted row count, and dry-run mode.

namespace Explore.Application.Models;

public sealed record IdempotencyCleanupResult(
    DateTime ExpiresBeforeUtc,
    int EligibleCount,
    int DeletedCount,
    bool DryRun);

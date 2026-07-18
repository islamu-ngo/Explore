// ABOUTME: Result model for one bounded email dispatch retention-redaction pass.
// ABOUTME: Reports cutoff, eligible and redacted counts, and dry-run mode without recipient data.

namespace Explore.Application.Models;

public sealed record EmailDispatchRetentionCleanupResult(
    DateTime CutoffUtc,
    int TenantCount,
    int SucceededTenantCount,
    int FailedTenantCount,
    int EligibleCount,
    int RedactedCount,
    bool DryRun);

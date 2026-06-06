// ABOUTME: Application contract for tenant-scoped AI assistant retention cleanup orchestration.
// ABOUTME: Keeps hosted scheduling independent from retention redaction and metrics implementation.

using Explore.Application.Models;

namespace Explore.Application.Contracts.Services;

public interface IAiRetentionCleanupService
{
    Task<AiRetentionCleanupRunResult> CleanupAllTenantsAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}

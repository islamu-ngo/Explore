// ABOUTME: Application contract for bounded email dispatch retention and content redaction.
// ABOUTME: Keeps cleanup orchestration independent from API hosted-service scheduling.

using Explore.Application.Models;

namespace Explore.Application.Contracts.Services;

public interface IEmailDispatchRetentionCleanupService
{
    Task<EmailDispatchRetentionCleanupResult> CleanupAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}

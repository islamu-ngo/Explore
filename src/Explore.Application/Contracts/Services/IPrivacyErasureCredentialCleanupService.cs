// ABOUTME: Application contract for bounded destruction of expired privacy-erasure credentials.
// ABOUTME: Keeps scheduled cleanup orchestration separate from persistence and API worker lifecycles.

using Explore.Application.Models;

namespace Explore.Application.Contracts.Services;

public interface IPrivacyErasureCredentialCleanupService
{
    Task<PrivacyErasureCredentialCleanupResult> CleanupAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}

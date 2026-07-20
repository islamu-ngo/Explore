// ABOUTME: Adapts API startup replay to the retained-authority platform erasure workflow.
// ABOUTME: Reuses the atomic checkpoint, tombstone, correction-outbox, and cache boundary.

using Explore.Application.Contracts.Services;

namespace Explore.Infrastructure.Services.Privacy;

public sealed class PrivacyErasureReplayService(
    IPrivacyErasureService erasureService) : IPrivacyErasureReplayService
{
    public Task ReplayAsync(CancellationToken cancellationToken) =>
        erasureService.ReplayPendingAsync(cancellationToken);
}

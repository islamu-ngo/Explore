// ABOUTME: Adapts API startup replay to the existing retained-authority erasure workflow.
// ABOUTME: Reuses the atomic checkpoint, tombstone, correction-outbox, and cache boundary.

using Explore.Application.Contracts.Services;

namespace Explore.Infrastructure.Services.Privacy;

public sealed class LocationErasureReplayService(
    IGlobalLocationPrivacyErasureService erasureService) : ILocationErasureReplayService
{
    public Task ReplayAsync(CancellationToken cancellationToken) =>
        erasureService.ReplayPendingAsync(cancellationToken);
}

// ABOUTME: No-op review queue provider for LocalOnly and disabled external review queue modes.
// ABOUTME: Reports disabled queue mirroring without attempting outbound provider synchronization.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.EventReporting.Models;

namespace Explore.Infrastructure.Services.Moderation;

public sealed class NoopReviewQueueProvider : IReviewQueueProvider
{
    public Task<ReviewCaseSyncResult> MirrorCaseAsync(
        ReviewCaseEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ReviewCaseSyncResult.Disabled("External review queue provider is disabled."));
    }
}

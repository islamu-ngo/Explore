// ABOUTME: Application boundary for mirroring report cases to optional external review queues.
// ABOUTME: Infrastructure implementations decide whether to no-op, local-sync, or call Coop-style providers.

using Explore.Application.Features.EventReporting.Models;

namespace Explore.Application.Contracts.Infrastructure;

public interface IReviewQueueProvider
{
    Task<ReviewCaseSyncResult> MirrorCaseAsync(
        ReviewCaseEnvelope envelope,
        CancellationToken cancellationToken = default);
}

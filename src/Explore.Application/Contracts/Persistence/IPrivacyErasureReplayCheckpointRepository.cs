// ABOUTME: Append-only local persistence contract for privacy-erasure replay checkpoints.
// ABOUTME: Reads the latest immutable authority sequence without exposing database query primitives.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IPrivacyErasureReplayCheckpointRepository
{
    Task<PrivacyErasureReplayCheckpoint> AppendAsync(
        PrivacyErasureReplayCheckpoint checkpoint,
        CancellationToken cancellationToken);
    Task<PrivacyErasureReplayCheckpoint?> GetLatestAsync(CancellationToken cancellationToken);
}

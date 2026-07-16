// ABOUTME: Append-only local persistence contract for privacy-erasure replay checkpoints.
// ABOUTME: Reads the latest immutable authority sequence without exposing database query primitives.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface ILocationPrivacyErasureReplayCheckpointRepository
{
    Task<LocationPrivacyErasureReplayCheckpoint> AppendAsync(
        LocationPrivacyErasureReplayCheckpoint checkpoint,
        CancellationToken cancellationToken);
    Task<LocationPrivacyErasureReplayCheckpoint?> GetLatestAsync(CancellationToken cancellationToken);
}

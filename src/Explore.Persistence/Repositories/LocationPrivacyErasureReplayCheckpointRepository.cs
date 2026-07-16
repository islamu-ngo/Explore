// ABOUTME: Appends and reads the application database's immutable erasure replay checkpoint chain.
// ABOUTME: Rejects duplicate, skipped, or forked authority sequences before database constraints run.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class LocationPrivacyErasureReplayCheckpointRepository(ExploreDbContext dbContext)
    : ILocationPrivacyErasureReplayCheckpointRepository
{
    public async Task<LocationPrivacyErasureReplayCheckpoint> AppendAsync(
        LocationPrivacyErasureReplayCheckpoint checkpoint,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        LocationPrivacyErasureReplayCheckpoint? latest =
            await GetLatestAsync(cancellationToken);
        bool startsChain = latest is null
            && checkpoint.AuthoritySequence == 1
            && checkpoint.PreviousCheckpointId is null;
        bool continuesChain = latest is not null
            && checkpoint.AuthoritySequence == latest.AuthoritySequence + 1
            && checkpoint.PreviousCheckpointId == latest.Id;
        if (!startsChain && !continuesChain)
        {
            throw new InvalidOperationException(
                "A local erasure checkpoint must append the next contiguous authority sequence.");
        }

        dbContext.LocationPrivacyErasureReplayCheckpoints.Add(checkpoint);
        await dbContext.SaveChangesAsync(cancellationToken);
        return checkpoint;
    }

    public Task<LocationPrivacyErasureReplayCheckpoint?> GetLatestAsync(
        CancellationToken cancellationToken) =>
        dbContext.LocationPrivacyErasureReplayCheckpoints
            .AsNoTracking()
            .OrderByDescending(item => item.AuthoritySequence)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
}

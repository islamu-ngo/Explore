// ABOUTME: Defines the CarpaNet-free boundary for fetching one complete bounded ATProto repository snapshot.
// ABOUTME: Returns domain-owned canonical items only after the remote repository is fully verified.

using Explore.Application.Features.Federation.Atproto.Models;

namespace Explore.Application.Contracts.Infrastructure;

public interface IAtprotoPdsSnapshotGateway
{
    Task<AtprotoPdsSnapshotFetchResult> FetchAsync(
        string did,
        long snapshotVersion,
        CancellationToken cancellationToken);
}

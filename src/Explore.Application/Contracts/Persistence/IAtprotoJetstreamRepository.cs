// ABOUTME: Repository contract for the single global fenced Jetstream lease and atomic cursor materialization.
// ABOUTME: Advances a cursor only with its canonical record, tombstone, presentations, or quarantine outcome.

using Explore.Domain;
using Explore.Domain.Federation;

namespace Explore.Application.Contracts.Persistence;

public sealed record AtprotoJetstreamClaim(
    Guid ConsumerStateId,
    string Service,
    long Cursor,
    Guid LeaseToken,
    long LeaseFence);

public sealed record AtprotoJetstreamApplyRequest(
    AtprotoJetstreamClaim Claim,
    long ExpectedCursor,
    long NextCursor,
    AtprotoRecord? Record,
    IReadOnlyList<AtprotoRecordTenantPresentation> Presentations,
    AtprotoJetstreamQuarantine? Quarantine,
    DateTime ObservedAt);

public interface IAtprotoJetstreamRepository
{
    Task<AtprotoJetstreamClaim?> TryClaimAsync(
        string service,
        string leaseOwner,
        DateTime claimedAt,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    Task<bool> TryRenewAsync(
        AtprotoJetstreamClaim claim,
        DateTime observedAt,
        DateTime leaseExpiresAt,
        CancellationToken cancellationToken = default);

    Task<bool> TryApplyAndAdvanceAsync(
        AtprotoJetstreamApplyRequest request,
        CancellationToken cancellationToken = default);
}

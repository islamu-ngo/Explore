// ABOUTME: Repository contract for the single global fenced Jetstream lease and atomic cursor materialization.
// ABOUTME: Applies records or quarantine atomically while allowing invalid cursor evidence to retain the last safe checkpoint.

using Explore.Application.Features.Federation.Atproto.Models;
using Explore.Application.Models.Storage;
using Explore.Domain;
using Explore.Domain.Federation;

namespace Explore.Application.Contracts.Persistence;

public sealed record AtprotoJetstreamClaim(
    Guid ConsumerStateId,
    string Service,
    long Cursor,
    Guid LeaseToken,
    long LeaseFence);

public sealed record AtprotoEventProjectionInvalidation(
    string Did,
    string Collection,
    string RecordKey,
    long SourceVersion);

public sealed record AtprotoJetstreamApplyRequest(
    AtprotoJetstreamClaim Claim,
    long ExpectedCursor,
    long NextCursor,
    AtprotoRecord? Record,
    IReadOnlyList<AtprotoRecordTenantPresentation> Presentations,
    AtprotoJetstreamQuarantine? Quarantine,
    DateTime ObservedAt,
    bool AdvanceCursor = true,
    AtprotoEventProjection? EventProjection = null,
    AtprotoEventProjectionInvalidation? EventProjectionInvalidation = null)
{
    public IReadOnlyList<AtprotoFederatedEventImportPlan> EventImports { get; init; } = [];
}

public sealed record AtprotoPersistenceApplyResult(
    bool Applied,
    IReadOnlyList<FileStorageWriteResult> ConsumedStagedThumbnails)
{
    public static AtprotoPersistenceApplyResult Rejected { get; } = new(false, []);
}

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

    Task<AtprotoPersistenceApplyResult> TryApplyAndAdvanceWithResultAsync(
        AtprotoJetstreamApplyRequest request,
        CancellationToken cancellationToken = default);
}

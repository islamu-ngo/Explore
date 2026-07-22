// ABOUTME: Implements the single global renewable Jetstream lease and fenced cursor ownership.
// ABOUTME: Atomically applies canonical records, tombstones, tenant presentations, or quarantine before cursor advance.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Federation.Atproto.Models;
using Explore.Domain;
using Explore.Domain.Federation;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class AtprotoJetstreamRepository : IAtprotoJetstreamRepository, IAtprotoPdsSnapshotRepository
{
    private const string EventCollection = "community.lexicon.calendar.event";
    private const string RsvpCollection = "community.lexicon.calendar.rsvp";
    private const string TidAlphabet = "234567abcdefghijklmnopqrstuvwxyz";
    private readonly ExploreDbContext _dbContext;

    public AtprotoJetstreamRepository(ExploreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AtprotoJetstreamClaim?> TryClaimAsync(
        string service,
        string leaseOwner,
        DateTime claimedAt,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        var normalizedService = service.Trim();
        var normalizedOwner = leaseOwner.Trim();
        if (normalizedService.Length is 0 or > 500 || normalizedOwner.Length is 0 or > 200 ||
            claimedAt.Kind != DateTimeKind.Utc || leaseDuration <= TimeSpan.Zero)
        {
            return null;
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            await AcquireConsumerLockAsync(normalizedService, cancellationToken);

            var state = await _dbContext.AtprotoJetstreamConsumerStates
                .SingleOrDefaultAsync(value => value.Service == normalizedService, cancellationToken);
            if (state is null)
            {
                state = new AtprotoJetstreamConsumerState
                {
                    Id = Guid.CreateVersion7(),
                    Service = normalizedService,
                    UpdatedAt = claimedAt
                };
                await _dbContext.AtprotoJetstreamConsumerStates.AddAsync(state, cancellationToken);
            }
            else if (state.LeaseExpiresAt > claimedAt)
            {
                await transaction.CommitAsync(cancellationToken);
                return null;
            }

            state.LeaseOwner = normalizedOwner;
            state.LeaseToken = Guid.CreateVersion7();
            state.LeaseExpiresAt = claimedAt.Add(leaseDuration);
            state.LeaseFence = checked(state.LeaseFence + 1);
            state.UpdatedAt = claimedAt;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            var claim = new AtprotoJetstreamClaim(
                state.Id,
                state.Service,
                state.Cursor,
                state.LeaseToken.Value,
                state.LeaseFence);
            _dbContext.Entry(state).State = EntityState.Detached;
            return claim;
        });
    }

    public async Task<bool> TryRenewAsync(
        AtprotoJetstreamClaim claim,
        DateTime observedAt,
        DateTime leaseExpiresAt,
        CancellationToken cancellationToken = default)
    {
        if (observedAt.Kind != DateTimeKind.Utc || leaseExpiresAt.Kind != DateTimeKind.Utc || leaseExpiresAt <= observedAt)
        {
            return false;
        }

        var affected = await _dbContext.AtprotoJetstreamConsumerStates
            .Where(value =>
                value.Id == claim.ConsumerStateId &&
                value.Service == claim.Service &&
                value.LeaseToken == claim.LeaseToken &&
                value.LeaseFence == claim.LeaseFence &&
                value.LeaseExpiresAt > observedAt)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(value => value.LeaseExpiresAt, leaseExpiresAt)
                .SetProperty(value => value.UpdatedAt, observedAt), cancellationToken);
        return affected == 1;
    }

    public async Task<bool> TryApplyAndAdvanceAsync(
        AtprotoJetstreamApplyRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ObservedAt.Kind != DateTimeKind.Utc || request.NextCursor <= request.ExpectedCursor ||
            (request.Record is null) == (request.Quarantine is null) ||
            (!request.AdvanceCursor && request.Quarantine?.ReasonCode != "invalid_cursor"))
        {
            return false;
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            await AcquireConsumerLockAsync(request.Claim.Service, cancellationToken);
            var state = await _dbContext.AtprotoJetstreamConsumerStates.SingleOrDefaultAsync(value =>
                value.Id == request.Claim.ConsumerStateId &&
                value.Service == request.Claim.Service &&
                value.Cursor == request.ExpectedCursor &&
                value.LeaseToken == request.Claim.LeaseToken &&
                value.LeaseFence == request.Claim.LeaseFence &&
                value.LeaseExpiresAt > request.ObservedAt,
                cancellationToken);
            if (state is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            if (request.Record is not null && !await ApplyRecordAsync(request, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            if (request.EventProjectionInvalidation is not null)
            {
                await InvalidateEventProjectionAsync(request, cancellationToken);
            }

            if (request.Quarantine is not null)
            {
                bool alreadyQuarantined = await _dbContext.AtprotoJetstreamQuarantines.AnyAsync(value =>
                    value.ConsumerStateId == state.Id && value.Cursor == request.NextCursor,
                    cancellationToken);
                if (!alreadyQuarantined)
                {
                    request.Quarantine.ConsumerStateId = state.Id;
                    request.Quarantine.Cursor = request.NextCursor;
                    await _dbContext.AtprotoJetstreamQuarantines.AddAsync(request.Quarantine, cancellationToken);
                }
            }

            state.Cursor = request.AdvanceCursor ? request.NextCursor : request.ExpectedCursor;
            state.LastEventAt = request.ObservedAt;
            state.UpdatedAt = request.ObservedAt;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        });
    }

    public async Task<bool> TryReconcileAsync(
        AtprotoPdsSnapshotApplyRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsValidSnapshotRequest(request))
        {
            return false;
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        bool firstAttempt = true;
        return await strategy.ExecuteAsync(async () =>
        {
            if (!firstAttempt)
            {
                _dbContext.ChangeTracker.Clear();
            }
            firstAttempt = false;

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            await AcquireConsumerLockAsync(request.Claim.Service, cancellationToken);
            AtprotoJetstreamConsumerState? state = await _dbContext.AtprotoJetstreamConsumerStates
                .SingleOrDefaultAsync(value =>
                    value.Id == request.Claim.ConsumerStateId
                    && value.Service == request.Claim.Service
                    && value.LeaseToken == request.Claim.LeaseToken
                    && value.LeaseFence == request.Claim.LeaseFence
                    && value.LeaseExpiresAt > request.ObservedAt,
                    cancellationToken);
            if (state is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            string[] scannedDids = request.ScannedDids.ToArray();
            Dictionary<string, AtprotoPdsSnapshot> snapshots = request.Snapshots
                .ToDictionary(value => value.Did, StringComparer.Ordinal);
            HashSet<(string Did, string Collection, string RecordKey)> present = request.Snapshots
                .SelectMany(snapshot => snapshot.PresentIdentities.Select(identity =>
                    (snapshot.Did, identity.Collection, identity.RecordKey)))
                .ToHashSet();
            HashSet<(string Did, string Collection, string RecordKey)> accepted = request.Snapshots
                .SelectMany(snapshot => snapshot.Items.Select(item =>
                    (snapshot.Did, item.Record.Collection, item.Record.RecordKey)))
                .ToHashSet();
            List<AtprotoRecord> canonicalRecords = await _dbContext.AtprotoRecords
                .Where(value =>
                    scannedDids.Contains(value.Did)
                    && (value.Collection == EventCollection || value.Collection == RsvpCollection))
                .ToListAsync(cancellationToken);
            Dictionary<(string Did, string Collection, string RecordKey), AtprotoRecord> canonicalByIdentity =
                canonicalRecords.ToDictionary(value => (value.Did, value.Collection, value.RecordKey));
            AtprotoRecord[] missing = canonicalRecords
                .Where(value =>
                    value.Direction != AtprotoRecordDirection.Outbound
                    && value.SourceVersion < request.SnapshotVersion
                    && !present.Contains((value.Did, value.Collection, value.RecordKey)))
                .ToArray();
            string[] missingEventUris = missing
                .Where(value => value.Collection == EventCollection && value.Uri is not null)
                .Select(value => value.Uri!)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var dependentCandidates = missingEventUris.Length == 0
                ? []
                : await _dbContext.AtprotoRecords
                    .Where(value =>
                        value.Collection == RsvpCollection
                        && value.SubjectUri != null
                        && missingEventUris.Contains(value.SubjectUri)
                        && value.SourceVersion < request.SnapshotVersion)
                    .Select(value => new
                    {
                        value.Id,
                        value.Did,
                        value.Collection,
                        value.RecordKey
                    })
                    .ToArrayAsync(cancellationToken);
            Guid[] dependentRecordIds = dependentCandidates
                .Where(value => !accepted.Contains((value.Did, value.Collection, value.RecordKey)))
                .Select(value => value.Id)
                .ToArray();
            Guid[] canonicalIds = canonicalRecords.Select(value => value.Id).ToArray();
            Dictionary<Guid, AtprotoEventProjection> projections = await _dbContext.AtprotoEventProjections
                .Where(value => canonicalIds.Contains(value.AtprotoRecordId))
                .ToDictionaryAsync(value => value.AtprotoRecordId, cancellationToken);
            Guid[] presentationRecordIds = canonicalIds.Concat(dependentRecordIds).Distinct().ToArray();
            List<AtprotoRecordTenantPresentation> presentations = await _dbContext
                .AtprotoRecordTenantPresentations
                .IgnoreTenantFilter(TenantFilterBypassReasons.AtprotoPdsSnapshotGlobalReconciliation)
                .Where(value => presentationRecordIds.Contains(value.AtprotoRecordId))
                .ToListAsync(cancellationToken);
            Dictionary<(Guid TenantId, Guid RecordId), AtprotoRecordTenantPresentation> presentationByKey =
                presentations.ToDictionary(value => (value.TenantId, value.AtprotoRecordId));
            HashSet<Guid> visibleTenantIds = request.PresentationTenantIds.ToHashSet();

            foreach (string did in scannedDids)
            {
                foreach (AtprotoPdsSnapshotItem item in snapshots[did].Items)
                {
                    var identity = (did, item.Record.Collection, item.Record.RecordKey);
                    if (!canonicalByIdentity.TryGetValue(identity, out AtprotoRecord? canonical))
                    {
                        canonical = item.Record;
                        canonical.Id = canonical.Id == Guid.Empty ? Guid.CreateVersion7() : canonical.Id;
                        canonical.Direction = AtprotoRecordDirection.Inbound;
                        canonical.Provenance = AtprotoRecordProvenance.Jetstream;
                        canonicalByIdentity.Add(identity, canonical);
                        await _dbContext.AtprotoRecords.AddAsync(canonical, cancellationToken);
                    }
                    else if (canonical.SourceVersion >= request.SnapshotVersion)
                    {
                        continue;
                    }
                    else
                    {
                        ApplySnapshotRecord(canonical, item.Record);
                    }

                    canonical.SourceVersion = request.SnapshotVersion;
                    canonical.SourceCursor = null;
                    canonical.UpdatedAt = request.ObservedAt;
                    canonical.TombstonedAt = null;
                    ApplySnapshotProjection(canonical, item.EventProjection, projections, request);
                    ReconcilePresentations(
                        canonical,
                        visibleTenantIds,
                        presentations,
                        presentationByKey,
                        request);
                }
            }

            foreach ((string did, string collection, string recordKey) in present.Except(accepted))
            {
                if (!canonicalByIdentity.TryGetValue((did, collection, recordKey), out AtprotoRecord? canonical)
                    || canonical.SourceVersion >= request.SnapshotVersion)
                {
                    continue;
                }

                if (collection == EventCollection
                    && projections.Remove(canonical.Id, out AtprotoEventProjection? projection))
                {
                    _dbContext.AtprotoEventProjections.Remove(projection);
                }

                HidePresentations(canonical.Id, presentations, request);
            }

            foreach (AtprotoRecord canonical in missing)
            {
                canonical.Cid = null;
                canonical.RecordJson = null;
                canonical.RecordHash = null;
                canonical.SubjectUri = null;
                canonical.SubjectCid = null;
                canonical.IndexedAt = request.ObservedAt;
                canonical.SourceVersion = request.SnapshotVersion;
                canonical.SourceCursor = null;
                canonical.UpdatedAt = request.ObservedAt;
                canonical.TombstonedAt = request.ObservedAt;
                if (canonical.Collection == EventCollection
                    && projections.Remove(canonical.Id, out AtprotoEventProjection? projection))
                {
                    _dbContext.AtprotoEventProjections.Remove(projection);
                }

                HidePresentations(canonical.Id, presentations, request);
            }

            foreach (Guid dependentId in dependentRecordIds)
            {
                HidePresentations(dependentId, presentations, request);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            if (!await HasCurrentFenceAtCommitAsync(request.Claim, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        });
    }

    private static bool IsValidSnapshotRequest(AtprotoPdsSnapshotApplyRequest request)
    {
        if (request.ObservedAt.Kind != DateTimeKind.Utc
            || request.SnapshotVersion <= 0
            || request.Claim.ConsumerStateId == Guid.Empty
            || request.Claim.LeaseToken == Guid.Empty
            || request.Claim.LeaseFence <= 0
            || request.Claim.Service is not { Length: > 0 and <= 500 }
            || request.ScannedDids is not { Count: > 0 and <= 100 }
            || request.Snapshots is null
            || request.PresentationTenantIds is null
            || request.Snapshots.Count != request.ScannedDids.Count
            || request.PresentationTenantIds.Any(value => value == Guid.Empty)
            || request.PresentationTenantIds.Count != request.PresentationTenantIds.Distinct().Count())
        {
            return false;
        }

        var scannedDids = new HashSet<string>(StringComparer.Ordinal);
        foreach (string did in request.ScannedDids)
        {
            if (!IsValidDid(did) || !scannedDids.Add(did))
            {
                return false;
            }
        }

        var snapshotDids = new HashSet<string>(StringComparer.Ordinal);
        foreach (AtprotoPdsSnapshot snapshot in request.Snapshots)
        {
            if (!scannedDids.Contains(snapshot.Did)
                || !snapshotDids.Add(snapshot.Did)
                || snapshot.PresentIdentities is null
                || snapshot.Items is null)
            {
                return false;
            }

            var present = new HashSet<(string Collection, string RecordKey)>();
            foreach (AtprotoPdsSnapshotIdentity identity in snapshot.PresentIdentities)
            {
                if (!IsExactCollection(identity.Collection)
                    || !IsValidProtocolRecordKey(identity.RecordKey)
                    || !present.Add((identity.Collection, identity.RecordKey)))
                {
                    return false;
                }
            }

            var accepted = new HashSet<(string Collection, string RecordKey)>();
            foreach (AtprotoPdsSnapshotItem item in snapshot.Items)
            {
                AtprotoRecord record = item.Record;
                var identity = (record.Collection, record.RecordKey);
                if (!string.Equals(record.Did, snapshot.Did, StringComparison.Ordinal)
                    || !IsExactCollection(record.Collection)
                    || !IsValidTid(record.RecordKey)
                    || !accepted.Add(identity)
                    || !present.Contains(identity)
                    || record.TombstonedAt is not null
                    || (record.Collection == EventCollection) != (item.EventProjection is not null))
                {
                    return false;
                }
            }
        }

        return snapshotDids.SetEquals(scannedDids);
    }

    private static bool IsValidDid(string did) =>
        did is { Length: > 4 and <= 255 }
        && did.StartsWith("did:", StringComparison.Ordinal)
        && !did.Any(character => char.IsWhiteSpace(character) || char.IsControl(character));

    private static bool IsExactCollection(string collection) =>
        collection is EventCollection or RsvpCollection;

    private static bool IsValidProtocolRecordKey(string recordKey) =>
        recordKey is { Length: > 0 and <= 512 }
        && recordKey is not "." and not ".."
        && recordKey.All(character =>
            char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_' or ':' or '~');

    private static bool IsValidTid(string recordKey) =>
        recordKey is { Length: 13 }
        && TidAlphabet.IndexOf(recordKey[0], StringComparison.Ordinal) is >= 0 and <= 15
        && recordKey.All(TidAlphabet.Contains);

    private static void ApplySnapshotRecord(AtprotoRecord canonical, AtprotoRecord supplied)
    {
        bool isEcho = canonical.Direction is AtprotoRecordDirection.Outbound or AtprotoRecordDirection.Reconciled;
        canonical.Direction = isEcho ? AtprotoRecordDirection.Reconciled : AtprotoRecordDirection.Inbound;
        canonical.Provenance = isEcho
            ? AtprotoRecordProvenance.JetstreamEcho
            : AtprotoRecordProvenance.Jetstream;
        canonical.Cid = supplied.Cid;
        canonical.Uri = supplied.Uri;
        canonical.RecordJson = supplied.RecordJson;
        canonical.RecordHash = supplied.RecordHash;
        canonical.SubjectUri = supplied.SubjectUri;
        canonical.SubjectCid = supplied.SubjectCid;
        canonical.IndexedAt = supplied.IndexedAt;
    }

    private void ApplySnapshotProjection(
        AtprotoRecord canonical,
        AtprotoEventProjection? supplied,
        IDictionary<Guid, AtprotoEventProjection> projections,
        AtprotoPdsSnapshotApplyRequest request)
    {
        if (canonical.Collection != EventCollection || supplied is null)
        {
            return;
        }

        if (!projections.TryGetValue(canonical.Id, out AtprotoEventProjection? existing))
        {
            supplied.AtprotoRecordId = canonical.Id;
            supplied.SourceVersion = request.SnapshotVersion;
            supplied.MaterializedAt = request.ObservedAt;
            projections.Add(canonical.Id, supplied);
            _dbContext.AtprotoEventProjections.Add(supplied);
            return;
        }

        existing.Name = supplied.Name;
        existing.Description = supplied.Description;
        existing.CreatedAt = supplied.CreatedAt;
        existing.StartsAt = supplied.StartsAt;
        existing.EndsAt = supplied.EndsAt;
        existing.Mode = supplied.Mode;
        existing.Status = supplied.Status;
        existing.RsvpExpected = supplied.RsvpExpected;
        existing.LocationSummary = supplied.LocationSummary;
        existing.SourceUrl = supplied.SourceUrl;
        existing.SourceVersion = request.SnapshotVersion;
        existing.MaterializedAt = request.ObservedAt;
    }

    private void ReconcilePresentations(
        AtprotoRecord canonical,
        IReadOnlySet<Guid> visibleTenantIds,
        ICollection<AtprotoRecordTenantPresentation> presentations,
        IDictionary<(Guid TenantId, Guid RecordId), AtprotoRecordTenantPresentation> presentationByKey,
        AtprotoPdsSnapshotApplyRequest request)
    {
        foreach (AtprotoRecordTenantPresentation presentation in presentations
                     .Where(value => value.AtprotoRecordId == canonical.Id))
        {
            presentation.IsVisible = visibleTenantIds.Contains(presentation.TenantId);
            presentation.SourceVersion = request.SnapshotVersion;
            presentation.EvaluatedAt = request.ObservedAt;
        }

        foreach (Guid tenantId in visibleTenantIds)
        {
            if (presentationByKey.ContainsKey((tenantId, canonical.Id)))
            {
                continue;
            }

            var presentation = new AtprotoRecordTenantPresentation
            {
                TenantId = tenantId,
                AtprotoRecordId = canonical.Id,
                IsVisible = true,
                SourceVersion = request.SnapshotVersion,
                EvaluatedAt = request.ObservedAt
            };
            presentations.Add(presentation);
            presentationByKey.Add((tenantId, canonical.Id), presentation);
            _dbContext.AtprotoRecordTenantPresentations.Add(presentation);
        }
    }

    private static void HidePresentations(
        Guid recordId,
        IEnumerable<AtprotoRecordTenantPresentation> presentations,
        AtprotoPdsSnapshotApplyRequest request)
    {
        foreach (AtprotoRecordTenantPresentation presentation in presentations
                     .Where(value => value.AtprotoRecordId == recordId))
        {
            presentation.IsVisible = false;
            presentation.SourceVersion = request.SnapshotVersion;
            presentation.EvaluatedAt = request.ObservedAt;
        }
    }

    private async Task AcquireConsumerLockAsync(string service, CancellationToken cancellationToken)
    {
        if (_dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            await _dbContext.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_xact_lock(hashtext({0}))",
                [$"atproto-jetstream:{service}"],
                cancellationToken);
        }
    }

    private async Task<bool> HasCurrentFenceAtCommitAsync(
        AtprotoJetstreamClaim claim,
        CancellationToken cancellationToken)
    {
        if (_dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            int affected = await _dbContext.Database.ExecuteSqlRawAsync(
                """
                UPDATE atproto_jetstream_consumer_states
                SET service = service
                WHERE id = {0}
                  AND service = {1}
                  AND lease_token = {2}
                  AND lease_fence = {3}
                  AND lease_expires_at > clock_timestamp()
                """,
                [claim.ConsumerStateId, claim.Service, claim.LeaseToken, claim.LeaseFence],
                cancellationToken);
            return affected == 1;
        }

        DateTime now = DateTime.UtcNow;
        return await _dbContext.AtprotoJetstreamConsumerStates.AnyAsync(value =>
            value.Id == claim.ConsumerStateId
            && value.Service == claim.Service
            && value.LeaseToken == claim.LeaseToken
            && value.LeaseFence == claim.LeaseFence
            && value.LeaseExpiresAt > now,
            cancellationToken);
    }

    private async Task<bool> ApplyRecordAsync(
        AtprotoJetstreamApplyRequest request,
        CancellationToken cancellationToken)
    {
        var incoming = request.Record!;
        var canonical = await _dbContext.AtprotoRecords.SingleOrDefaultAsync(value =>
            value.Did == incoming.Did &&
            value.Collection == incoming.Collection &&
            value.RecordKey == incoming.RecordKey,
            cancellationToken);
        if (canonical is not null && incoming.SourceVersion <= canonical.SourceVersion)
        {
            return true;
        }

        if (canonical is null)
        {
            canonical = incoming;
            if (canonical.Id == Guid.Empty)
            {
                canonical.Id = Guid.CreateVersion7();
            }
            canonical.Direction = AtprotoRecordDirection.Inbound;
            canonical.Provenance = AtprotoRecordProvenance.Jetstream;
            await _dbContext.AtprotoRecords.AddAsync(canonical, cancellationToken);
        }
        else
        {
            canonical.Direction = canonical.Direction is AtprotoRecordDirection.Outbound or AtprotoRecordDirection.Reconciled
                ? AtprotoRecordDirection.Reconciled
                : AtprotoRecordDirection.Inbound;
            canonical.Provenance = canonical.Direction == AtprotoRecordDirection.Reconciled
                ? AtprotoRecordProvenance.JetstreamEcho
                : AtprotoRecordProvenance.Jetstream;
            canonical.Cid = incoming.Cid;
            canonical.Uri = incoming.Uri;
            canonical.RecordJson = incoming.RecordJson;
            canonical.RecordHash = incoming.RecordHash;
            canonical.SubjectUri = incoming.SubjectUri;
            canonical.SubjectCid = incoming.SubjectCid;
            canonical.IndexedAt = incoming.IndexedAt;
            canonical.SourceVersion = incoming.SourceVersion;
            canonical.SourceCursor = request.NextCursor;
            canonical.UpdatedAt = request.ObservedAt;
            canonical.TombstonedAt = incoming.TombstonedAt;
        }

        canonical.SourceCursor = request.NextCursor;
        canonical.UpdatedAt = request.ObservedAt;

        if (canonical.Collection == "community.lexicon.calendar.event")
        {
            await ApplyEventProjectionAsync(canonical, request, cancellationToken);
        }

        foreach (var supplied in request.Presentations)
        {
            var presentation = await _dbContext.AtprotoRecordTenantPresentations
                .IgnoreTenantFilter(TenantFilterBypassReasons.AtprotoJetstreamGlobalMaterialization)
                .SingleOrDefaultAsync(value =>
                    value.TenantId == supplied.TenantId && value.AtprotoRecordId == canonical.Id,
                    cancellationToken);
            if (presentation is null)
            {
                supplied.AtprotoRecordId = canonical.Id;
                supplied.SourceVersion = canonical.SourceVersion;
                supplied.EvaluatedAt = request.ObservedAt;
                await _dbContext.AtprotoRecordTenantPresentations.AddAsync(supplied, cancellationToken);
            }
            else
            {
                presentation.IsVisible = supplied.IsVisible && canonical.TombstonedAt is null;
                presentation.SourceVersion = canonical.SourceVersion;
                presentation.EvaluatedAt = request.ObservedAt;
            }
        }

        if (canonical.TombstonedAt is not null)
        {
            await _dbContext.AtprotoRecordTenantPresentations
                .IgnoreTenantFilter(TenantFilterBypassReasons.AtprotoJetstreamGlobalMaterialization)
                .Where(value => value.AtprotoRecordId == canonical.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(value => value.IsVisible, false)
                    .SetProperty(value => value.EvaluatedAt, request.ObservedAt), cancellationToken);

            if (canonical.Uri is not null)
            {
                var dependentIds = _dbContext.AtprotoRecords
                    .Where(value => value.SubjectUri == canonical.Uri)
                    .Select(value => value.Id);
                await _dbContext.AtprotoRecordTenantPresentations
                    .IgnoreTenantFilter(TenantFilterBypassReasons.AtprotoJetstreamGlobalMaterialization)
                    .Where(value => dependentIds.Contains(value.AtprotoRecordId))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(value => value.IsVisible, false)
                        .SetProperty(value => value.EvaluatedAt, request.ObservedAt), cancellationToken);
            }
        }

        return true;
    }

    private async Task ApplyEventProjectionAsync(
        AtprotoRecord canonical,
        AtprotoJetstreamApplyRequest request,
        CancellationToken cancellationToken)
    {
        AtprotoEventProjection? existing = await _dbContext.AtprotoEventProjections
            .SingleOrDefaultAsync(value => value.AtprotoRecordId == canonical.Id, cancellationToken);
        if (canonical.TombstonedAt is not null)
        {
            if (existing is not null)
            {
                _dbContext.AtprotoEventProjections.Remove(existing);
            }
            return;
        }

        AtprotoEventProjection? supplied = request.EventProjection;
        if (supplied is null || supplied.SourceVersion != canonical.SourceVersion)
        {
            return;
        }

        if (existing is null)
        {
            supplied.AtprotoRecordId = canonical.Id;
            await _dbContext.AtprotoEventProjections.AddAsync(supplied, cancellationToken);
            return;
        }

        existing.Name = supplied.Name;
        existing.Description = supplied.Description;
        existing.CreatedAt = supplied.CreatedAt;
        existing.StartsAt = supplied.StartsAt;
        existing.EndsAt = supplied.EndsAt;
        existing.Mode = supplied.Mode;
        existing.Status = supplied.Status;
        existing.RsvpExpected = supplied.RsvpExpected;
        existing.LocationSummary = supplied.LocationSummary;
        existing.SourceUrl = supplied.SourceUrl;
        existing.SourceVersion = supplied.SourceVersion;
        existing.MaterializedAt = supplied.MaterializedAt;
    }

    private async Task InvalidateEventProjectionAsync(
        AtprotoJetstreamApplyRequest request,
        CancellationToken cancellationToken)
    {
        AtprotoEventProjectionInvalidation invalidation = request.EventProjectionInvalidation!;
        AtprotoRecord? canonical = await _dbContext.AtprotoRecords.SingleOrDefaultAsync(value =>
            value.Did == invalidation.Did
            && value.Collection == invalidation.Collection
            && value.RecordKey == invalidation.RecordKey,
            cancellationToken);
        if (canonical is null || invalidation.SourceVersion <= canonical.SourceVersion)
        {
            return;
        }

        AtprotoEventProjection? projection = await _dbContext.AtprotoEventProjections
            .SingleOrDefaultAsync(value => value.AtprotoRecordId == canonical.Id, cancellationToken);
        if (projection is not null)
        {
            _dbContext.AtprotoEventProjections.Remove(projection);
        }

        await _dbContext.AtprotoRecordTenantPresentations
            .IgnoreTenantFilter(TenantFilterBypassReasons.AtprotoJetstreamGlobalMaterialization)
            .Where(value => value.AtprotoRecordId == canonical.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(value => value.IsVisible, false)
                .SetProperty(value => value.SourceVersion, invalidation.SourceVersion)
                .SetProperty(value => value.EvaluatedAt, request.ObservedAt), cancellationToken);
    }
}

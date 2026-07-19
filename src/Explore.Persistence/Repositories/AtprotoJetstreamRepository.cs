// ABOUTME: Implements the single global renewable Jetstream lease and fenced cursor ownership.
// ABOUTME: Atomically applies canonical records, tombstones, tenant presentations, or quarantine before cursor advance.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Federation;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class AtprotoJetstreamRepository : IAtprotoJetstreamRepository
{
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
            if (_dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                await _dbContext.Database.ExecuteSqlRawAsync(
                    "SELECT pg_advisory_xact_lock(hashtext({0}))",
                    [$"atproto-jetstream:{normalizedService}"],
                    cancellationToken);
            }

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

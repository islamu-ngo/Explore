// ABOUTME: Implements tenant-owned PDS outbox enqueue, reclaimable fenced claims, supersession, and settlement.
// ABOUTME: Settles canonical URI/CID, ownership, presentation, and terminal outbox state in one transaction.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Federation;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class PdsSyncOutboxRepository : IPdsSyncOutboxRepository
{
    private readonly ExploreDbContext _dbContext;

    public PdsSyncOutboxRepository(ExploreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PdsSyncOutbox outbox, CancellationToken cancellationToken = default) =>
        await _dbContext.PdsSyncOutbox.AddAsync(outbox, cancellationToken);

    public async Task<IReadOnlyList<PdsSyncClaim>> ClaimDueAsync(
        int batchSize,
        string leaseOwner,
        DateTime claimedAt,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        if (batchSize is < 1 or > 100 || claimedAt.Kind != DateTimeKind.Utc || leaseDuration <= TimeSpan.Zero)
        {
            return [];
        }

        var normalizedOwner = leaseOwner.Trim();
        if (normalizedOwner.Length is 0 or > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(leaseOwner));
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            if (_dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                await _dbContext.Database.ExecuteSqlRawAsync(
                    "SELECT pg_advisory_xact_lock(hashtext({0}))",
                    ["atproto-pds-outbox-claim"],
                    cancellationToken);
            }

            var candidates = await CrossTenantOutbox()
                .Where(value =>
                    (value.Status == PdsSyncStatus.Pending &&
                     (value.NextRetryAt == null || value.NextRetryAt <= claimedAt) ||
                     value.Status == PdsSyncStatus.Processing && value.LeaseExpiresAt <= claimedAt) &&
                    value.SupersededAt == null &&
                    (value.DependsOnAtprotoRecordId == null ||
                     _dbContext.AtprotoRecords.Any(record =>
                         record.Id == value.DependsOnAtprotoRecordId &&
                         record.Uri != null &&
                         record.Cid != null &&
                         record.TombstonedAt == null)))
                .OrderBy(value => value.CreatedAt)
                .ThenBy(value => value.Id)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            var claims = new List<PdsSyncClaim>(candidates.Count);
            foreach (var outbox in candidates)
            {
                outbox.Status = PdsSyncStatus.Processing;
                outbox.LeaseOwner = normalizedOwner;
                outbox.LeaseToken = Guid.CreateVersion7();
                outbox.LeaseExpiresAt = claimedAt.Add(leaseDuration);
                outbox.LeaseFence = checked(outbox.LeaseFence + 1);
                claims.Add(new PdsSyncClaim(
                    outbox.Id,
                    outbox.TenantId,
                    outbox.UserId,
                    outbox.LeaseToken.Value,
                    outbox.LeaseFence));
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();
            return claims;
        });
    }

    public Task<PdsSyncOutbox?> GetActiveClaimAsync(
        PdsSyncClaim claim,
        DateTime observedAt,
        CancellationToken cancellationToken = default) =>
        CrossTenantOutbox()
            .AsNoTracking()
            .SingleOrDefaultAsync(value =>
                value.Id == claim.OutboxId &&
                value.TenantId == claim.TenantId &&
                value.UserId == claim.UserId &&
                value.Status == PdsSyncStatus.Processing &&
                value.LeaseToken == claim.LeaseToken &&
                value.LeaseFence == claim.LeaseFence &&
                value.LeaseExpiresAt > observedAt,
                cancellationToken);

    public async Task<bool> TryRenewClaimAsync(
        PdsSyncClaim claim,
        DateTime observedAt,
        DateTime leaseExpiresAt,
        CancellationToken cancellationToken = default)
    {
        if (observedAt.Kind != DateTimeKind.Utc || leaseExpiresAt.Kind != DateTimeKind.Utc || leaseExpiresAt <= observedAt)
        {
            return false;
        }

        var affected = await CrossTenantOutbox()
            .Where(value =>
                value.Id == claim.OutboxId &&
                value.TenantId == claim.TenantId &&
                value.UserId == claim.UserId &&
                value.Status == PdsSyncStatus.Processing &&
                value.LeaseToken == claim.LeaseToken &&
                value.LeaseFence == claim.LeaseFence &&
                value.LeaseExpiresAt > observedAt)
            .ExecuteUpdateAsync(setters => setters.SetProperty(value => value.LeaseExpiresAt, leaseExpiresAt), cancellationToken);
        return affected == 1;
    }

    public async Task<bool> TrySettleAsync(
        PdsSyncClaim claim,
        string? uri,
        string? cid,
        DateTime settledAt,
        CancellationToken cancellationToken = default)
    {
        if (settledAt.Kind != DateTimeKind.Utc)
        {
            return false;
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var outbox = await ActiveClaimQuery(claim, settledAt).SingleOrDefaultAsync(cancellationToken);
            if (outbox is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var record = await _dbContext.AtprotoRecords.SingleOrDefaultAsync(value =>
                value.Did == outbox.Did &&
                value.Collection == outbox.Collection &&
                value.RecordKey == outbox.RecordKey,
                cancellationToken);

            if (outbox.Operation == PdsSyncOperation.Delete)
            {
                if (record?.Uri is null || record.Cid is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return false;
                }

                record.TombstonedAt = settledAt;
                record.UpdatedAt = settledAt;
                uri = record.Uri;
                cid = record.Cid;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(uri) || string.IsNullOrWhiteSpace(cid))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return false;
                }

                if (outbox.ExpectedCid is not null && record?.Cid != outbox.ExpectedCid)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return false;
                }

                if (record is null)
                {
                    record = new AtprotoRecord
                    {
                        Id = Guid.CreateVersion7(),
                        Did = outbox.Did,
                        Collection = outbox.Collection,
                        RecordKey = outbox.RecordKey,
                        Direction = AtprotoRecordDirection.Outbound,
                        Provenance = AtprotoRecordProvenance.LocalLifecycle,
                        UpdatedAt = settledAt
                    };
                    await _dbContext.AtprotoRecords.AddAsync(record, cancellationToken);
                }
                else
                {
                    record.Direction = record.Direction == AtprotoRecordDirection.Inbound
                        ? AtprotoRecordDirection.Reconciled
                        : AtprotoRecordDirection.Outbound;
                    record.Provenance = record.Direction == AtprotoRecordDirection.Reconciled
                        ? AtprotoRecordProvenance.JetstreamEcho
                        : AtprotoRecordProvenance.LocalLifecycle;
                }

                if (record.Uri is not null && !string.Equals(record.Uri, uri, StringComparison.Ordinal))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return false;
                }

                record.Uri = uri;
                record.Cid = cid;
                record.RecordJson = outbox.Payload;
                record.RecordHash = outbox.PayloadHash;
                record.IndexedAt = settledAt;
                record.UpdatedAt = settledAt;
                record.TombstonedAt = null;
            }

            var ownership = await _dbContext.AtprotoOutboundRecordOwnerships
                .IgnoreTenantFilter(TenantFilterBypassReasons.AtprotoTenantOperation)
                .SingleOrDefaultAsync(value => value.AtprotoRecordId == record.Id, cancellationToken);
            if (ownership is null)
            {
                ownership = new AtprotoOutboundRecordOwnership
                {
                    AtprotoRecordId = record.Id,
                    TenantId = outbox.TenantId,
                    UserId = outbox.UserId,
                    SourceEntityType = outbox.SourceEntityType,
                    SourceEntityId = outbox.SourceEntityId,
                    SourceVersion = outbox.SourceVersion,
                    CreatedAt = settledAt,
                    UpdatedAt = settledAt
                };
                await _dbContext.AtprotoOutboundRecordOwnerships.AddAsync(ownership, cancellationToken);
            }
            else if (ownership.TenantId != outbox.TenantId || ownership.UserId != outbox.UserId ||
                     ownership.SourceEntityType != outbox.SourceEntityType || ownership.SourceEntityId != outbox.SourceEntityId)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }
            else
            {
                ownership.SourceVersion = outbox.SourceVersion;
                ownership.UpdatedAt = settledAt;
            }

            var presentation = await _dbContext.AtprotoRecordTenantPresentations
                .IgnoreTenantFilter(TenantFilterBypassReasons.AtprotoTenantOperation)
                .SingleOrDefaultAsync(value =>
                    value.TenantId == outbox.TenantId && value.AtprotoRecordId == record.Id,
                    cancellationToken);
            if (presentation is null)
            {
                await _dbContext.AtprotoRecordTenantPresentations.AddAsync(
                    new AtprotoRecordTenantPresentation
                    {
                        TenantId = outbox.TenantId,
                        AtprotoRecordId = record.Id,
                        IsVisible = outbox.Operation != PdsSyncOperation.Delete,
                        SourceVersion = record.SourceVersion,
                        EvaluatedAt = settledAt
                    },
                    cancellationToken);
            }
            else
            {
                presentation.IsVisible = outbox.Operation != PdsSyncOperation.Delete;
                presentation.SourceVersion = record.SourceVersion;
                presentation.EvaluatedAt = settledAt;
            }

            outbox.AtprotoRecordId = record.Id;
            outbox.SettledUri = uri;
            outbox.SettledCid = cid;
            outbox.Status = PdsSyncStatus.Completed;
            outbox.ProcessedAt = settledAt;
            outbox.LastError = null;
            ClearLease(outbox);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        });
    }

    public async Task<bool> TryFailAsync(
        PdsSyncClaim claim,
        string failureCode,
        bool retryable,
        DateTime failedAt,
        TimeSpan retryDelay,
        CancellationToken cancellationToken = default)
    {
        var normalizedFailure = failureCode.Trim();
        if (failedAt.Kind != DateTimeKind.Utc || normalizedFailure.Length is 0 or > 100 ||
            !normalizedFailure.All(character =>
                character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_' or '-') ||
            retryDelay < TimeSpan.Zero)
        {
            return false;
        }

        var outbox = await ActiveClaimQuery(claim, failedAt).SingleOrDefaultAsync(cancellationToken);
        if (outbox is null)
        {
            return false;
        }

        outbox.RetryCount++;
        outbox.LastError = normalizedFailure;
        if (retryable && outbox.RetryCount < outbox.MaxRetries)
        {
            outbox.Status = PdsSyncStatus.Pending;
            outbox.NextRetryAt = failedAt.Add(retryDelay);
        }
        else
        {
            outbox.Status = PdsSyncStatus.DeadLettered;
            outbox.DeadLetteredAt = failedAt;
            outbox.NextRetryAt = null;
        }

        ClearLease(outbox);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> SupersedePriorAsync(
        Guid tenantId,
        string sourceEntityType,
        Guid sourceEntityId,
        Guid supersedingOutboxId,
        DateTime supersededAt,
        CancellationToken cancellationToken = default)
    {
        var rows = await CrossTenantOutbox()
            .Where(value =>
                value.TenantId == tenantId &&
                value.SourceEntityType == sourceEntityType &&
                value.SourceEntityId == sourceEntityId &&
                value.Id != supersedingOutboxId &&
                value.Status != PdsSyncStatus.Completed &&
                value.Status != PdsSyncStatus.Superseded)
            .ToListAsync(cancellationToken);
        foreach (var row in rows)
        {
            row.Status = PdsSyncStatus.Superseded;
            row.SupersededById = supersedingOutboxId;
            row.SupersededAt = supersededAt;
            ClearLease(row);
        }

        return rows.Count;
    }

    private IQueryable<PdsSyncOutbox> CrossTenantOutbox() =>
        _dbContext.PdsSyncOutbox.IgnoreTenantFilter(TenantFilterBypassReasons.AtprotoPdsWorkerCrossTenantQueue);

    private IQueryable<PdsSyncOutbox> ActiveClaimQuery(PdsSyncClaim claim, DateTime observedAt) =>
        CrossTenantOutbox().Where(value =>
            value.Id == claim.OutboxId &&
            value.TenantId == claim.TenantId &&
            value.UserId == claim.UserId &&
            value.Status == PdsSyncStatus.Processing &&
            value.LeaseToken == claim.LeaseToken &&
            value.LeaseFence == claim.LeaseFence &&
            value.LeaseExpiresAt > observedAt &&
            value.SupersededAt == null);

    private static void ClearLease(PdsSyncOutbox outbox)
    {
        outbox.LeaseOwner = null;
        outbox.LeaseToken = null;
        outbox.LeaseExpiresAt = null;
    }
}

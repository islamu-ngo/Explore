// ABOUTME: EF Core repository for durable notification fanout run state.
// ABOUTME: Supports idempotent source lookup and background worker polling for internal fanout.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class NotificationFanoutRunRepository : GenericRepository<NotificationFanoutRun, Guid>, INotificationFanoutRunRepository
{
    private readonly ExploreDbContext _dbContext;

    public NotificationFanoutRunRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<NotificationFanoutRun?> GetBySourceAsync(
        Guid tenantId,
        string fanoutKind,
        int notificationEntityTypeId,
        Guid entityId,
        Guid sourceActorId,
        bool trackChanges = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.NotificationFanoutRuns
            .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
            .Include(run => run.NotificationEntityType)
            .Include(run => run.SourceActor)
                .ThenInclude(actor => actor.Pii)
            .Where(run => run.TenantId == tenantId
                && run.FanoutKind == fanoutKind
                && run.NotificationEntityTypeId == notificationEntityTypeId
                && run.EntityId == entityId
                && run.SourceActorId == sourceActorId);

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<NotificationFanoutRun>> GetPendingBatchAsync(
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageSize <= 0)
        {
            return [];
        }

        return await _dbContext.NotificationFanoutRuns
            .IgnoreTenantFilter(TenantFilterBypassReasons.NotificationFanoutWorkerCrossTenantQueue)
            .AsNoTracking()
            .Where(run => run.Status == "pending")
            .OrderBy(run => run.CreatedAt)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<NotificationFanoutRun?> GetByOccurrenceAsync(
        Guid tenantId,
        Guid occurrenceId,
        bool trackChanges = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<NotificationFanoutRun> query = _dbContext.NotificationFanoutRuns
            .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
            .Where(run => run.TenantId == tenantId && run.FanoutOccurrenceId == occurrenceId);
        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<NotificationFanoutRun?> EnsurePendingOccurrenceRunAsync(
        Guid tenantId,
        Guid occurrenceId,
        Guid runId,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || occurrenceId == Guid.Empty || runId == Guid.Empty)
        {
            throw new ArgumentException("Fanout run identifiers must be non-empty.");
        }

        Guid concurrencyStamp = Guid.CreateVersion7();
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            try
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                await AcquireOccurrenceLockAsync(tenantId, occurrenceId, cancellationToken);

                var occurrence = await LoadPendingOccurrenceSourceAsync(
                    tenantId,
                    occurrenceId,
                    notAfter: null,
                    cancellationToken);
                if (occurrence is null)
                {
                    await transaction.CommitAsync(cancellationToken);
                    return null;
                }

                var existing = await _dbContext.NotificationFanoutRuns
                    .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        item => item.TenantId == tenantId && item.FanoutOccurrenceId == occurrenceId,
                        cancellationToken);
                if (existing is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                    return existing;
                }

                var run = CreateOccurrenceRun(
                    runId,
                    tenantId,
                    occurrenceId,
                    occurrence.EventId,
                    occurrence.ActorId,
                    concurrencyStamp);
                await _dbContext.NotificationFanoutRuns.AddAsync(run, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return run;
            }
            catch
            {
                _dbContext.ChangeTracker.Clear();
                throw;
            }
        });
    }

    public async Task<NotificationFanoutClaim?> TryClaimOccurrenceAsync(
        Guid tenantId,
        Guid occurrenceId,
        string leaseOwner,
        DateTime claimedAt,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || occurrenceId == Guid.Empty
            || claimedAt.Kind != DateTimeKind.Utc || leaseDuration <= TimeSpan.Zero
            || string.IsNullOrWhiteSpace(leaseOwner))
        {
            return null;
        }

        string normalizedOwner = leaseOwner.Trim();
        if (normalizedOwner.Length > NotificationFanoutRun.MaxLeaseOwnerLength)
        {
            throw new ArgumentOutOfRangeException(nameof(leaseOwner));
        }

        Guid runId = Guid.CreateVersion7();
        Guid concurrencyStamp = Guid.CreateVersion7();
        Guid leaseToken = Guid.CreateVersion7();
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            try
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                await AcquireOccurrenceLockAsync(tenantId, occurrenceId, cancellationToken);

                var occurrence = await LoadPendingOccurrenceSourceAsync(
                    tenantId,
                    occurrenceId,
                    claimedAt,
                    cancellationToken);
                if (occurrence is null)
                {
                    await transaction.CommitAsync(cancellationToken);
                    return null;
                }

                var run = await _dbContext.NotificationFanoutRuns
                    .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
                    .SingleOrDefaultAsync(
                        item => item.TenantId == tenantId && item.FanoutOccurrenceId == occurrenceId,
                        cancellationToken);
                if (run is null)
                {
                    run = CreateOccurrenceRun(
                        runId,
                        tenantId,
                        occurrenceId,
                        occurrence.EventId,
                        occurrence.ActorId,
                        concurrencyStamp);
                    await _dbContext.NotificationFanoutRuns.AddAsync(run, cancellationToken);
                }

                bool hasActiveLease = run.Status == "processing" && run.ProcessingLeaseExpiresAt > claimedAt;
                bool ownsActiveLease = hasActiveLease
                    && run.ProcessingLeaseOwner == normalizedOwner
                    && run.ProcessingLeaseToken == leaseToken;
                if (run.Status == "completed" || (hasActiveLease && !ownsActiveLease))
                {
                    await transaction.CommitAsync(cancellationToken);
                    _dbContext.Entry(run).State = EntityState.Detached;
                    return null;
                }

                if (!ownsActiveLease)
                {
                    run.Status = "processing";
                    run.ProcessingLeaseOwner = normalizedOwner;
                    run.ProcessingLeaseToken = leaseToken;
                    run.ProcessingLeaseExpiresAt = claimedAt.Add(leaseDuration);
                    run.HeartbeatAt = claimedAt;
                    run.StartedAt ??= claimedAt;
                    run.ProcessingGeneration = checked(run.ProcessingGeneration + 1);
                    run.ProcessingFence = checked(run.ProcessingFence + 1);
                    run.FailedAt = null;
                    run.LastError = null;
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                await transaction.CommitAsync(cancellationToken);

                NotificationFanoutAudienceCursor? cursor = run.CursorFirstEligibleRegistrationCreatedAt.HasValue
                    && run.CursorUserId.HasValue
                    ? new NotificationFanoutAudienceCursor(
                        run.CursorFirstEligibleRegistrationCreatedAt.Value,
                        run.CursorUserId.Value)
                    : null;
                var claim = new NotificationFanoutClaim(
                    run.Id,
                    run.TenantId,
                    occurrenceId,
                    leaseToken,
                    run.ProcessingFence,
                    run.ProcessingGeneration,
                    cursor);
                _dbContext.Entry(run).State = EntityState.Detached;
                return claim;
            }
            catch
            {
                _dbContext.ChangeTracker.Clear();
                throw;
            }
        });
    }

    public async Task<bool> TryRenewClaimAsync(
        NotificationFanoutClaim claim,
        DateTime observedAt,
        DateTime leaseExpiresAt,
        CancellationToken cancellationToken)
    {
        if (observedAt.Kind != DateTimeKind.Utc || leaseExpiresAt.Kind != DateTimeKind.Utc
            || leaseExpiresAt <= observedAt)
        {
            return false;
        }

        int affected = await ActiveClaimQuery(claim, observedAt)
            .Where(run => run.ProcessingLeaseExpiresAt < leaseExpiresAt)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(run => run.ProcessingLeaseExpiresAt, leaseExpiresAt)
                .SetProperty(run => run.HeartbeatAt, observedAt)
                .SetProperty(run => run.UpdatedAt, observedAt), cancellationToken);
        return affected == 1;
    }

    public async Task<bool> TryCheckpointAsync(
        NotificationFanoutClaim claim,
        NotificationFanoutAudienceCursor? expectedCursor,
        NotificationFanoutAudienceCursor nextCursor,
        int processedDelta,
        int createdDelta,
        DateTime observedAt,
        CancellationToken cancellationToken)
    {
        if (observedAt.Kind != DateTimeKind.Utc || processedDelta < 0 || createdDelta < 0
            || createdDelta > processedDelta || !IsAfter(nextCursor, expectedCursor))
        {
            return false;
        }

        DateTime? expectedTimestamp = expectedCursor?.FirstEligibleRegistrationCreatedAt;
        Guid? expectedUserId = expectedCursor?.UserId;
        int affected = await ActiveClaimQuery(claim, observedAt)
            .Where(run =>
                (expectedTimestamp == null
                    && run.CursorFirstEligibleRegistrationCreatedAt == null
                    && run.CursorUserId == null)
                || (expectedTimestamp != null
                    && run.CursorFirstEligibleRegistrationCreatedAt == expectedTimestamp
                    && run.CursorUserId == expectedUserId))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(run => run.CursorFirstEligibleRegistrationCreatedAt,
                    nextCursor.FirstEligibleRegistrationCreatedAt)
                .SetProperty(run => run.CursorUserId, nextCursor.UserId)
                .SetProperty(run => run.ProcessedCount, run => run.ProcessedCount + processedDelta)
                .SetProperty(run => run.CreatedNotificationCount,
                    run => run.CreatedNotificationCount + createdDelta)
                .SetProperty(run => run.HeartbeatAt, observedAt)
                .SetProperty(run => run.UpdatedAt, observedAt), cancellationToken);
        return affected == 1;
    }

    public async Task<bool> TryCompleteAsync(
        NotificationFanoutClaim claim,
        DateTime observedAt,
        CancellationToken cancellationToken)
    {
        if (observedAt.Kind != DateTimeKind.Utc)
        {
            return false;
        }

        int affected = await ActiveClaimQuery(claim, observedAt)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(run => run.Status, "completed")
                .SetProperty(run => run.CompletedAt, observedAt)
                .SetProperty(run => run.HeartbeatAt, observedAt)
                .SetProperty(run => run.ProcessingLeaseOwner, (string?)null)
                .SetProperty(run => run.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(run => run.ProcessingLeaseExpiresAt, (DateTime?)null)
                .SetProperty(run => run.UpdatedAt, observedAt), cancellationToken);
        return affected == 1;
    }

    private IQueryable<NotificationFanoutRun> ActiveClaimQuery(
        NotificationFanoutClaim claim,
        DateTime observedAt) =>
        _dbContext.NotificationFanoutRuns
            .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
            .Where(run => run.TenantId == claim.TenantId
                && run.Id == claim.RunId
                && run.FanoutOccurrenceId == claim.OccurrenceId
                && run.Status == "processing"
                && run.ProcessingLeaseToken == claim.LeaseToken
                && run.ProcessingFence == claim.Fence
                && run.ProcessingGeneration == claim.Generation
                && run.ProcessingLeaseExpiresAt > observedAt);

    private async Task AcquireOccurrenceLockAsync(
        Guid tenantId,
        Guid occurrenceId,
        CancellationToken cancellationToken)
    {
        if (_dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            await _dbContext.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_xact_lock(hashtext({0}))",
                [$"notification-fanout:{tenantId:N}:{occurrenceId:N}"],
                cancellationToken);
        }
    }

    private Task<OccurrenceSource?> LoadPendingOccurrenceSourceAsync(
        Guid tenantId,
        Guid occurrenceId,
        DateTime? notAfter,
        CancellationToken cancellationToken) =>
        _dbContext.NotificationFanoutOccurrences
            .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId
                && item.Id == occurrenceId
                && item.State == NotificationFanoutOccurrenceState.Pending
                && (notAfter == null || item.NotBefore <= notAfter))
            .Join(
                _dbContext.Events
                    .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
                    .AsNoTracking(),
                item => new { item.TenantId, item.EventId },
                item => new { item.TenantId, EventId = item.Id },
                (item, eventEntity) => new OccurrenceSource(item.EventId, eventEntity.ActorId))
            .SingleOrDefaultAsync(cancellationToken);

    private static NotificationFanoutRun CreateOccurrenceRun(
        Guid runId,
        Guid tenantId,
        Guid occurrenceId,
        Guid eventId,
        Guid sourceActorId,
        Guid concurrencyStamp) =>
        new()
        {
            Id = runId,
            TenantId = tenantId,
            Tenant = null!,
            FanoutOccurrenceId = occurrenceId,
            FanoutOccurrence = null,
            FanoutKind = "recipient_occurrence",
            NotificationEntityTypeId = (int)NotificationEntityTypeEnum.Event,
            NotificationEntityType = null!,
            EntityId = eventId,
            SourceActorId = sourceActorId,
            SourceActor = null!,
            Status = "pending",
            ConcurrencyStamp = concurrencyStamp
        };

    private static bool IsAfter(
        NotificationFanoutAudienceCursor next,
        NotificationFanoutAudienceCursor? current) =>
        current is null
        || next.FirstEligibleRegistrationCreatedAt > current.Value.FirstEligibleRegistrationCreatedAt
        || (next.FirstEligibleRegistrationCreatedAt == current.Value.FirstEligibleRegistrationCreatedAt
            && next.UserId.CompareTo(current.Value.UserId) > 0);

    private sealed record OccurrenceSource(Guid EventId, Guid ActorId);
}

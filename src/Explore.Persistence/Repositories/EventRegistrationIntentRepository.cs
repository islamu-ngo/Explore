// ABOUTME: EF implementation of capacity-aware registration parent and child persistence.
// ABOUTME: Requires caller-owned serializable coordination and translates only exact registration deduplication conflicts.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Federation;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Explore.Persistence.Repositories;

public class EventRegistrationIntentRepository : GenericRepository<EventRegistrationIntent, Guid>, IEventRegistrationIntentRepository
{
    private const string UniqueViolationSqlState = "23505";
    private const string EventScopeUniqueIndexName = "ix_event_registration_intents_unique_event_scope";
    private const string DayScopeUniqueIndexName = "ix_event_registration_intents_unique_day_scope";
    private const string SessionSelectionScopeUniqueIndexName = "ix_event_registration_intents_unique_session_selection_scope";

    private readonly ExploreDbContext _dbContext;

    public EventRegistrationIntentRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<EventRegistrationIntent?> GetAtprotoLifecycleStateAsync(
        Guid tenantId,
        Guid intentId,
        CancellationToken cancellationToken) =>
        _dbContext.EventRegistrationIntents
            .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
            .IgnoreQueryFilters([QueryFilterNames.SoftDelete])
            .AsNoTracking()
            .SingleOrDefaultAsync(intent =>
                intent.TenantId == tenantId && intent.Id == intentId,
                cancellationToken);

    public Task<int> CountActiveForEventUserAsync(
        Guid tenantId,
        Guid eventId,
        Guid userId,
        CancellationToken cancellationToken) =>
        _dbContext.EventRegistrationIntents
            .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
            .AsNoTracking()
            .CountAsync(intent =>
                intent.TenantId == tenantId
                && intent.EventId == eventId
                && intent.UserId == userId,
                cancellationToken);

    public async Task<IReadOnlyList<EventRegistrationIntent>> GetAtprotoReconciliationCandidatesAsync(
        Guid? afterIntentId,
        int batchSize,
        CancellationToken cancellationToken)
    {
        if (batchSize is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize));
        }

        IQueryable<EventRegistrationIntent> query = _dbContext.EventRegistrationIntents
            .IgnoreAllFilters(TenantFilterBypassReasons.AtprotoPdsWorkerCrossTenantQueue)
            .AsNoTracking()
            .Where(intent =>
                !intent.IsDeleted
                && _dbContext.AtprotoOutboundRecordOwnerships
                    .IgnoreTenantFilter(TenantFilterBypassReasons.AtprotoPdsWorkerCrossTenantQueue)
                    .Any(ownership =>
                        ownership.TenantId == intent.TenantId
                        && ownership.SourceEntityType == "Event"
                        && ownership.SourceEntityId == intent.EventId
                        && ownership.AtprotoRecord != null
                        && ownership.AtprotoRecord.Uri != null
                        && ownership.AtprotoRecord.Cid != null
                        && ownership.AtprotoRecord.TombstonedAt == null)
                && !_dbContext.AtprotoOutboundRecordOwnerships
                    .IgnoreTenantFilter(TenantFilterBypassReasons.AtprotoPdsWorkerCrossTenantQueue)
                    .Any(ownership =>
                        ownership.TenantId == intent.TenantId
                        && ownership.UserId == intent.UserId
                        && ownership.SourceEntityType == "EventRegistrationIntent"
                        && ownership.AtprotoRecord != null
                        && ownership.AtprotoRecord.Uri != null
                        && ownership.AtprotoRecord.Cid != null
                        && ownership.AtprotoRecord.TombstonedAt == null
                        && _dbContext.EventRegistrationIntents
                            .IgnoreAllFilters(TenantFilterBypassReasons.AtprotoPdsWorkerCrossTenantQueue)
                            .Any(source =>
                                source.Id == ownership.SourceEntityId
                                && source.TenantId == intent.TenantId
                                && source.UserId == intent.UserId
                                && source.EventId == intent.EventId))
                && !_dbContext.PdsSyncOutbox
                    .IgnoreTenantFilter(TenantFilterBypassReasons.AtprotoPdsWorkerCrossTenantQueue)
                    .Any(outbox =>
                        outbox.TenantId == intent.TenantId
                        && outbox.SourceEntityType == "EventRegistrationIntent"
                        && outbox.Collection == "community.lexicon.calendar.rsvp"
                        && (outbox.Operation == PdsSyncOperation.Create
                            || outbox.Operation == PdsSyncOperation.Update)
                        && (outbox.Status == PdsSyncStatus.Pending
                            || outbox.Status == PdsSyncStatus.Processing)
                        && outbox.SupersededAt == null
                        && _dbContext.EventRegistrationIntents
                            .IgnoreAllFilters(TenantFilterBypassReasons.AtprotoPdsWorkerCrossTenantQueue)
                            .Any(source =>
                                source.Id == outbox.SourceEntityId
                                && source.TenantId == intent.TenantId
                                && source.UserId == intent.UserId
                                && source.EventId == intent.EventId)));
        query = query.Where(intent => !_dbContext.PdsSyncOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.AtprotoPdsWorkerCrossTenantQueue)
            .Any(outbox =>
                outbox.TenantId == intent.TenantId
                && outbox.SourceEntityType == "EventRegistrationIntent"
                && outbox.Collection == "community.lexicon.calendar.rsvp"
                && (outbox.Operation == PdsSyncOperation.Create
                    || outbox.Operation == PdsSyncOperation.Update)
                && outbox.Status == PdsSyncStatus.DeadLettered
                && outbox.SupersededAt == null
                && outbox.SourceVersion == intent.ConcurrencyStamp
                && outbox.DependsOnCid != null
                && outbox.DependsOnAtprotoRecord != null
                && outbox.DependsOnAtprotoRecord.Cid == outbox.DependsOnCid
                && outbox.DependsOnAtprotoRecord.TombstonedAt == null
                && _dbContext.EventRegistrationIntents
                    .IgnoreAllFilters(TenantFilterBypassReasons.AtprotoPdsWorkerCrossTenantQueue)
                    .Any(source =>
                        source.Id == outbox.SourceEntityId
                        && source.TenantId == intent.TenantId
                        && source.UserId == intent.UserId
                        && source.EventId == intent.EventId)));

        IQueryable<EventRegistrationIntent> representatives = query.Where(intent => !query.Any(other =>
            other.TenantId == intent.TenantId
            && other.UserId == intent.UserId
            && other.EventId == intent.EventId
            && other.Id.CompareTo(intent.Id) < 0));
        if (afterIntentId is { } cursor)
        {
            representatives = representatives.Where(intent => intent.Id.CompareTo(cursor) > 0);
        }

        return await representatives
            .OrderBy(intent => intent.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<EventRegistrationIntent?> FindExistingAsync(
        Guid eventId,
        Guid userId,
        int registrationScopeId,
        Guid? selectedEventDayId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.EventRegistrationIntents
            .AsNoTracking()
            .FirstOrDefaultAsync(
                i => i.EventId == eventId
                    && i.UserId == userId
                    && i.RegistrationScopeId == registrationScopeId
                    && i.SelectedEventDayId == selectedEventDayId,
                cancellationToken);
    }

    public async Task<EventRegistrationIntentCreationResult> CreateWithChildrenAndCapacityAsync(
        EventRegistrationIntent intent,
        IReadOnlyList<EventRegistration> children,
        int approvedStatusId,
        int waitlistedStatusId,
        Guid occurrenceId,
        DateTimeOffset occurredAt,
        EventRegistrationActorProvenance actorProvenance,
        Guid? actorUserId,
        CancellationToken cancellationToken,
        IntegrationSyncOutbox? integrationSyncOutbox = null)
    {
        if (_dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory"
            && _dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "Capacity-aware registration creation requires a caller-owned serializable transaction.");
        }

        try
        {
            await ValidateChildrenBelongToIntentAsync(intent, children, cancellationToken);

            var waitlistedSessionIds = new List<Guid>();

            foreach (var child in children)
            {
                child.CoverageEstablishedAt = occurredAt.UtcDateTime;

                var reserved = await TryReserveSessionCapacityAsync(
                    intent.TenantId,
                    intent.EventId,
                    child.EventSessionId,
                    cancellationToken);
                child.ApprovalStatusId = reserved ? approvedStatusId : waitlistedStatusId;

                if (!reserved)
                {
                    waitlistedSessionIds.Add(child.EventSessionId);
                }
            }

            intent.ApprovalStatusId = waitlistedSessionIds.Count == 0
                ? approvedStatusId
                : waitlistedStatusId;

            await _dbContext.EventRegistrationIntents.AddAsync(intent, cancellationToken);

            foreach (var child in children)
            {
                child.EventRegistrationIntentId = intent.Id;
                child.EventId = intent.EventId;
                await _dbContext.EventRegistrations.AddAsync(child, cancellationToken);
            }

            if (integrationSyncOutbox is not null)
            {
                integrationSyncOutbox.RegistrationIntentId = intent.Id;
                integrationSyncOutbox.SourceId = intent.Id;
                await _dbContext.IntegrationSyncOutbox.AddAsync(integrationSyncOutbox, cancellationToken);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            var childTransitions = children
                .Select(child => new EventRegistrationChildTransition(
                    child.Id,
                    child.EventSessionId,
                    PreviousStatus: null,
                    child.ApprovalStatusId))
                .ToArray();
            var transition = new EventRegistrationTransitionResult(
                Changed: true,
                ParentIntentId: intent.Id,
                PreviousStatus: null,
                FinalStatus: intent.ApprovalStatusId,
                TransitionReason: waitlistedSessionIds.Count == 0
                    ? EventRegistrationTransitionReason.Created
                    : EventRegistrationTransitionReason.CapacityWaitlisted,
                OccurrenceId: occurrenceId,
                OccurredAt: occurredAt,
                ActorProvenance: actorProvenance,
                ActorUserId: actorUserId,
                ChildTransitions: childTransitions);

            return new EventRegistrationIntentCreationResult(intent, waitlistedSessionIds, transition);
        }
        catch (DbUpdateException ex) when (IsDuplicateIntentViolation(ex))
        {
            throw new EventRegistrationIntentConflictException(ex);
        }
    }

    public async Task<IReadOnlyList<Guid>> GetRegisteredUserFanoutBatchAsync(
        Guid tenantId,
        Guid eventId,
        Guid? afterUserId,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<Guid> query = _dbContext.EventRegistrationIntents
            .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
            .AsNoTracking()
            .Where(intent => intent.TenantId == tenantId
                && intent.EventId == eventId
                && !intent.IsDeleted
                && (intent.ApprovalStatusId == (int)ApprovalStatusEnum.Pending
                    || intent.ApprovalStatusId == (int)ApprovalStatusEnum.Approved
                    || intent.ApprovalStatusId == (int)ApprovalStatusEnum.Waitlisted))
            .Select(intent => intent.UserId)
            .Distinct();

        if (afterUserId.HasValue)
        {
            query = query.Where(userId => userId.CompareTo(afterUserId.Value) > 0);
        }

        return await query
            .OrderBy(userId => userId)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationFanoutAudienceMember>> GetNotificationFanoutAudienceBatchAsync(
        Guid tenantId,
        Guid eventId,
        Guid? sessionId,
        DateTime audienceCutoffAt,
        int deliveryPolicyId,
        NotificationFanoutAudienceCursor? after,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || eventId == Guid.Empty)
        {
            throw new ArgumentException("Tenant and event identifiers are required.");
        }

        if (audienceCutoffAt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Audience cutoff must use UTC kind.", nameof(audienceCutoffAt));
        }

        if (pageSize is < 1 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }

        int[] eligibleStatusIds = deliveryPolicyId switch
        {
            (int)NotificationDeliveryPolicyEnum.CriticalEventUpdateOptional or
            (int)NotificationDeliveryPolicyEnum.ModerationAvailabilityRequired or
            (int)NotificationDeliveryPolicyEnum.ModerationContextOptional =>
            [
                (int)ApprovalStatusEnum.Pending,
                (int)ApprovalStatusEnum.Approved,
                (int)ApprovalStatusEnum.Waitlisted
            ],
            (int)NotificationDeliveryPolicyEnum.ReminderOptional =>
                [(int)ApprovalStatusEnum.Approved],
            _ => throw new ArgumentOutOfRangeException(
                nameof(deliveryPolicyId),
                "The delivery policy does not define an attendee fanout audience.")
        };

        IQueryable<NotificationFanoutAudienceMember> audience;
        if (sessionId is null)
        {
            audience = _dbContext.EventRegistrationIntents
                .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
                .AsNoTracking()
                .Where(intent => intent.TenantId == tenantId
                    && intent.EventId == eventId
                    && intent.CreatedAt <= audienceCutoffAt
                    && intent.ApprovalStatusId.HasValue
                    && eligibleStatusIds.Contains(intent.ApprovalStatusId.Value)
                    && _dbContext.TenantUsers
                        .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
                        .Any(member => member.TenantId == tenantId
                            && member.UserId == intent.UserId
                            && member.StatusId == (int)TenantUserStatusEnum.Active)
                    && _dbContext.Users.Any(user => user.Id == intent.UserId))
                .GroupBy(intent => intent.UserId)
                .Select(group => new NotificationFanoutAudienceMember
                {
                    UserId = group.Key,
                    FirstEligibleRegistrationCreatedAt = group.Min(intent => intent.CreatedAt)
                });
        }
        else
        {
            audience =
                from child in _dbContext.EventRegistrations
                    .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
                    .AsNoTracking()
                join intent in _dbContext.EventRegistrationIntents
                        .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
                        .AsNoTracking()
                    on new { child.TenantId, child.EventId, child.EventRegistrationIntentId }
                    equals new
                    {
                        intent.TenantId,
                        intent.EventId,
                        EventRegistrationIntentId = (Guid?)intent.Id
                    }
                where child.TenantId == tenantId
                    && child.EventId == eventId
                    && child.EventSessionId == sessionId.Value
                    && child.UserId == intent.UserId
                    && intent.CreatedAt <= audienceCutoffAt
                    && child.CoverageEstablishedAt <= audienceCutoffAt
                    && child.ApprovalStatusId.HasValue
                    && eligibleStatusIds.Contains(child.ApprovalStatusId.Value)
                    && intent.ApprovalStatusId.HasValue
                    && eligibleStatusIds.Contains(intent.ApprovalStatusId.Value)
                    && _dbContext.TenantUsers
                        .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
                        .Any(member => member.TenantId == tenantId
                            && member.UserId == intent.UserId
                            && member.StatusId == (int)TenantUserStatusEnum.Active)
                    && _dbContext.Users.Any(user => user.Id == intent.UserId)
                group child by intent.UserId
                into cohort
                select new NotificationFanoutAudienceMember
                {
                    UserId = cohort.Key,
                    FirstEligibleRegistrationCreatedAt = cohort.Min(child => child.CoverageEstablishedAt)
                };
        }

        if (after is { } cursor)
        {
            audience = audience.Where(member =>
                member.FirstEligibleRegistrationCreatedAt > cursor.FirstEligibleRegistrationCreatedAt
                || (member.FirstEligibleRegistrationCreatedAt == cursor.FirstEligibleRegistrationCreatedAt
                    && member.UserId.CompareTo(cursor.UserId) > 0));
        }

        return await audience
            .OrderBy(member => member.FirstEligibleRegistrationCreatedAt)
            .ThenBy(member => member.UserId)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    private async Task ValidateChildrenBelongToIntentAsync(
        EventRegistrationIntent intent,
        IReadOnlyList<EventRegistration> children,
        CancellationToken cancellationToken)
    {
        var mismatchedChild = children.FirstOrDefault(child =>
            child.TenantId != intent.TenantId || child.EventId != intent.EventId);

        if (mismatchedChild is not null)
        {
            throw new InvalidOperationException(
                "Event registration child does not belong to the registration intent tenant and event.");
        }

        var requestedSessionIds = children
            .Select(child => child.EventSessionId)
            .Distinct()
            .ToArray();

        if (requestedSessionIds.Length == 0)
        {
            return;
        }

        var validSessionIds = await _dbContext.EventSessions
            .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
            .AsNoTracking()
            .Where(session => session.TenantId == intent.TenantId
                && session.EventId == intent.EventId
                && requestedSessionIds.Contains(session.Id))
            .Select(session => session.Id)
            .ToListAsync(cancellationToken);

        var validSessionIdSet = validSessionIds.ToHashSet();
        if (validSessionIdSet.Count == requestedSessionIds.Length)
        {
            return;
        }

        var invalidSessionId = requestedSessionIds.First(sessionId => !validSessionIdSet.Contains(sessionId));
        throw new InvalidOperationException(
            $"Event session {invalidSessionId} does not belong to the registration intent tenant and event.");
    }

    private async Task<bool> TryReserveSessionCapacityAsync(
        Guid tenantId,
        Guid eventId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var affectedRows = await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE event_sessions
            SET current_audience_attendees = COALESCE(current_audience_attendees, 0) + 1
            WHERE id = {sessionId}
              AND tenant_id = {tenantId}
              AND event_id = {eventId}
              AND is_deleted = false
              AND (
                  max_audience_attendees IS NULL
                  OR COALESCE(current_audience_attendees, 0) < max_audience_attendees
              )
            """, cancellationToken);

        return affectedRows > 0;
    }

    private static bool IsDuplicateIntentViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException
        {
            SqlState: UniqueViolationSqlState,
            ConstraintName: EventScopeUniqueIndexName or DayScopeUniqueIndexName or SessionSelectionScopeUniqueIndexName
        };
    }

}

// ABOUTME: EF Core repository for event registration reads, capacity-aware updates, and cancellation writes.
// ABOUTME: Requires caller-owned serializable transactions for atomic status, capacity, and cancellation changes.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Services.Registration;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Explore.Persistence.Repositories;

public class EventRegistrationRepository : GenericRepository<EventRegistration, Guid>, IEventRegistrationRepository
{
    private const string ActiveSessionRegistrationIndexName = "ix_eventregistrations_session_user";

    private readonly ExploreDbContext _dbContext;

    public EventRegistrationRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EventRegistration?> GetByIdWithDetails(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EventRegistrations
            .AsNoTracking()
            .Include(r => r.EventSession)
                .ThenInclude(s => s.Event)
                    .ThenInclude(e => e!.FeaturedImage)
            .Include(r => r.ApprovalStatus)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<EventRegistration?> GetRegistrationByUserAndSession(
        Guid userId,
        Guid eventSessionId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.EventRegistrations
            .AsNoTracking()
            .Include(r => r.EventSession)
                .ThenInclude(s => s.Event)
                    .ThenInclude(e => e!.FeaturedImage)
            .Include(r => r.ApprovalStatus)
            .FirstOrDefaultAsync(r => r.UserId == userId && r.EventSessionId == eventSessionId, cancellationToken);
    }

    public async Task<List<EventRegistration>> GetRegistrationsBySession(
        Guid eventSessionId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.EventRegistrations
            .AsNoTracking()
            .Include(r => r.EventSession)
                .ThenInclude(s => s.Event)
                    .ThenInclude(e => e!.FeaturedImage)
            .Include(r => r.ApprovalStatus)
            .Where(r => r.EventSessionId == eventSessionId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<EventRegistration>> GetRegistrationsByUser(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EventRegistrations
            .AsNoTracking()
            .Include(r => r.EventSession)
                .ThenInclude(s => s.Event)
                    .ThenInclude(e => e!.FeaturedImage)
            .Include(r => r.ApprovalStatus)
            .Where(r => r.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EventRegistration>> GetLocationAccessCoverageAsync(
        Guid tenantId,
        Guid eventId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || eventId == Guid.Empty || userId == Guid.Empty)
        {
            return [];
        }

        return await _dbContext.EventRegistrations
            .IgnoreQueryFilters([QueryFilterNames.SoftDelete])
            .AsNoTrackingWithIdentityResolution()
            .AsSingleQuery()
            .Include(registration => registration.EventRegistrationIntent)
            .Include(registration => registration.EventSession)
                .ThenInclude(session => session.EventLocation)
            .Include(registration => registration.Event)
                .ThenInclude(@event => @event.Sessions)
                    .ThenInclude(session => session.EventLocation)
            .Where(registration => registration.TenantId == tenantId
                && registration.EventId == eventId
                && registration.UserId == userId
                && registration.EventRegistrationIntentId != null
                && registration.EventRegistrationIntent != null
                && registration.EventRegistrationIntent.TenantId == tenantId
                && registration.EventRegistrationIntent.EventId == eventId
                && registration.EventRegistrationIntent.UserId == userId
                && registration.EventSession.TenantId == tenantId
                && registration.EventSession.EventId == eventId
                && registration.EventSession.EventLocationId != null
                && registration.Event.TenantId == tenantId
                && registration.Event.Id == eventId)
            .OrderBy(registration => registration.EventRegistrationIntentId)
            .ThenBy(registration => registration.EventSessionId)
            .ThenBy(registration => registration.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsUserRegisteredForSession(Guid userId, Guid eventSessionId)
    {
        return await _dbContext.EventRegistrations
            .AsNoTracking()
            .AnyAsync(r => r.UserId == userId && r.EventSessionId == eventSessionId);
    }

    public async Task<(List<EventRegistration> Items, int TotalCount)> GetRegistrationsByUserWithDetailsPaged(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.EventRegistrations
            .AsNoTracking()
            .AsSplitQuery()
            .Include(r => r.EventSession)
                .ThenInclude(s => s.Event)
                    .ThenInclude(e => e!.FeaturedImage)
            .Include(r => r.ApprovalStatus)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(List<EventRegistration> Items, int TotalCount)> GetRegistrationsByEventWithDetailsPaged(
        Guid eventId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.EventRegistrations
            .AsNoTracking()
            .AsSplitQuery()
            .Include(r => r.EventSession)
                .ThenInclude(s => s.Event)
                    .ThenInclude(e => e!.FeaturedImage)
            .Include(r => r.User)
            .Include(r => r.ApprovalStatus)
            .Where(r => r.EventId == eventId)
            .OrderByDescending(r => r.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<EventRegistrationTransitionResult> UpdateAndAdjustCapacityAsync(
        EventRegistration registration,
        Guid occurrenceId,
        DateTimeOffset occurredAt,
        EventRegistrationActorProvenance actorProvenance,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        RequireCallerOwnedSerializableTransaction();

        var entry = _dbContext.Entry(registration);
        if (entry.State == EntityState.Detached)
        {
            throw new InvalidOperationException("Capacity-aware registration updates require a tracked entity.");
        }

        var originalValues = entry.OriginalValues.Clone();
        var desiredValues = entry.CurrentValues.Clone();
        var originalApprovalStatusId = (int?)originalValues[nameof(EventRegistration.ApprovalStatusId)];
        var desiredApprovalStatusId = (int?)desiredValues[nameof(EventRegistration.ApprovalStatusId)];
        var originalSessionId = (Guid)originalValues[nameof(EventRegistration.EventSessionId)]!;
        var desiredSessionId = (Guid)desiredValues[nameof(EventRegistration.EventSessionId)]!;
        var originalEventId = (Guid)originalValues[nameof(EventRegistration.EventId)]!;
        var desiredEventId = (Guid)desiredValues[nameof(EventRegistration.EventId)]!;
        var originalTenantId = (Guid)originalValues[nameof(EventRegistration.TenantId)]!;
        var desiredTenantId = (Guid)desiredValues[nameof(EventRegistration.TenantId)]!;
        var originalIntentId = (Guid?)originalValues[nameof(EventRegistration.EventRegistrationIntentId)];
        var desiredIntentId = (Guid?)desiredValues[nameof(EventRegistration.EventRegistrationIntentId)];
        var originalUserId = (Guid)originalValues[nameof(EventRegistration.UserId)]!;
        var desiredUserId = (Guid)desiredValues[nameof(EventRegistration.UserId)]!;
        var originalAtprotoRecordId = (Guid?)originalValues[nameof(EventRegistration.AtprotoRecordId)];
        var desiredAtprotoRecordId = (Guid?)desiredValues[nameof(EventRegistration.AtprotoRecordId)];
        var originalCoverageEstablishedAt =
            (DateTime)originalValues[nameof(EventRegistration.CoverageEstablishedAt)]!;

        var previousParentStatus = originalIntentId.HasValue
            ? await _dbContext.EventRegistrationIntents
                .AsNoTracking()
                .Where(parent => parent.Id == originalIntentId.Value)
                .Select(parent => parent.ApprovalStatusId)
                .SingleOrDefaultAsync(cancellationToken)
            : originalApprovalStatusId;

        if (!RegistrationApprovalStatusRules.PreservesRegistrationIdentity(
                originalIntentId,
                desiredIntentId,
                originalUserId,
                desiredUserId,
                originalEventId,
                desiredEventId,
                originalTenantId,
                desiredTenantId))
        {
            throw new InvalidOperationException(
                "Registration user, event, tenant, and parent intent are immutable.");
        }

        if (!RegistrationApprovalStatusRules.CanTransition(
                originalApprovalStatusId,
                desiredApprovalStatusId))
        {
            throw new InvalidOperationException("Terminal registration approval statuses cannot be changed.");
        }

        var requestedChange = originalApprovalStatusId != desiredApprovalStatusId
            || originalSessionId != desiredSessionId
            || originalAtprotoRecordId != desiredAtprotoRecordId;
        if (!requestedChange)
        {
            return new EventRegistrationTransitionResult(
                Changed: false,
                ParentIntentId: originalIntentId,
                PreviousStatus: previousParentStatus,
                FinalStatus: previousParentStatus,
                TransitionReason: EventRegistrationTransitionReason.NoChange,
                OccurrenceId: occurrenceId,
                OccurredAt: occurredAt,
                ActorProvenance: actorProvenance,
                ActorUserId: actorUserId,
                ChildTransitions:
                [
                    new EventRegistrationChildTransition(
                        registration.Id,
                        registration.EventSessionId,
                        originalApprovalStatusId,
                        originalApprovalStatusId)
                ]);
        }

        var releaseOriginalCapacity = RegistrationApprovalStatusRules.IsCapacityBearing(originalApprovalStatusId)
            && (!RegistrationApprovalStatusRules.IsCapacityBearing(desiredApprovalStatusId)
                || originalSessionId != desiredSessionId);
        var reserveDesiredCapacity = RegistrationApprovalStatusRules.IsCapacityBearing(desiredApprovalStatusId)
            && (!RegistrationApprovalStatusRules.IsCapacityBearing(originalApprovalStatusId)
                || originalSessionId != desiredSessionId);
        var recomputeParentApprovalStatus = originalApprovalStatusId != desiredApprovalStatusId
            || reserveDesiredCapacity;
        var replacement = new EventRegistration
        {
            Id = occurrenceId,
            EventId = desiredEventId,
            Event = null!,
            UserId = desiredUserId,
            User = null!,
            EventSessionId = desiredSessionId,
            EventSession = null!,
            EventRegistrationIntentId = desiredIntentId,
            EventRegistrationIntent = null,
            ApprovalStatusId = desiredApprovalStatusId,
            ApprovalStatus = null,
            TenantId = desiredTenantId,
            Tenant = null!,
            AtprotoRecordId = desiredAtprotoRecordId,
            AtprotoRecord = null,
            CoverageEstablishedAt = originalSessionId == desiredSessionId
                ? originalCoverageEstablishedAt
                : occurredAt.UtcDateTime,
            CreatedBy = actorUserId,
            ConcurrencyStamp = Guid.CreateVersion7()
        };

        entry.CurrentValues.SetValues(originalValues);
        entry.OriginalValues.SetValues(originalValues);
        entry.State = EntityState.Deleted;
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (_dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            await AdjustInMemoryCapacityAsync(
                replacement,
                releaseOriginalCapacity,
                reserveDesiredCapacity,
                originalSessionId,
                desiredSessionId,
                cancellationToken);
        }
        else
        {
            if (releaseOriginalCapacity)
            {
                await ReleaseSessionCapacityAsync(
                    originalTenantId,
                    originalEventId,
                    originalSessionId,
                    cancellationToken);
            }

            if (reserveDesiredCapacity
                && !await TryReserveSessionCapacityAsync(
                    desiredTenantId,
                    desiredEventId,
                    desiredSessionId,
                    cancellationToken))
            {
                replacement.ApprovalStatusId = (int)ApprovalStatusEnum.Waitlisted;
            }
        }

        await _dbContext.EventRegistrations.AddAsync(replacement, cancellationToken);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: ActiveSessionRegistrationIndexName
        })
        {
            throw new ConcurrencyConflictException(
                ConcurrencyConflictException.ConcurrentUpdate,
                "The registration was modified by another request. Reload and retry.",
                nameof(EventRegistration),
                registration.Id.ToString(),
                exception);
        }

        EventRegistrationIntent? recomputedParent = null;
        if (recomputeParentApprovalStatus)
        {
            recomputedParent = await RecomputeParentApprovalStatusAsync(
                replacement,
                includeCurrentRegistration: true,
                noLiveChildStatusId: replacement.ApprovalStatusId,
                cancellationToken: cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var finalParentStatus = recomputedParent?.ApprovalStatusId ?? previousParentStatus;
        var changed = originalApprovalStatusId != replacement.ApprovalStatusId
            || originalSessionId != replacement.EventSessionId
            || originalAtprotoRecordId != replacement.AtprotoRecordId
            || previousParentStatus != finalParentStatus;
        var reason = !changed
            ? EventRegistrationTransitionReason.NoChange
            : replacement.ApprovalStatusId == (int)ApprovalStatusEnum.Revoked
                ? EventRegistrationTransitionReason.Revoked
                : replacement.ApprovalStatusId == (int)ApprovalStatusEnum.Waitlisted
                    && desiredApprovalStatusId != (int)ApprovalStatusEnum.Waitlisted
                    ? EventRegistrationTransitionReason.CapacityWaitlisted
                    : originalApprovalStatusId != replacement.ApprovalStatusId
                        || previousParentStatus != finalParentStatus
                        ? EventRegistrationTransitionReason.ApprovalStatusChanged
                        : EventRegistrationTransitionReason.Updated;

        return new EventRegistrationTransitionResult(
            Changed: changed,
            ParentIntentId: originalIntentId,
            PreviousStatus: previousParentStatus,
            FinalStatus: finalParentStatus,
            TransitionReason: reason,
            OccurrenceId: occurrenceId,
            OccurredAt: occurredAt,
            ActorProvenance: actorProvenance,
            ActorUserId: actorUserId,
            ChildTransitions:
            [
                new EventRegistrationChildTransition(
                    replacement.Id,
                    replacement.EventSessionId,
                    originalApprovalStatusId,
                    replacement.ApprovalStatusId)
            ]);
    }

    public async Task<EventRegistrationTransitionResult> CancelAndReleaseCapacityAsync(
        Guid registrationId,
        Guid expectedOwnerUserId,
        Guid occurrenceId,
        DateTimeOffset occurredAt,
        EventRegistrationActorProvenance actorProvenance,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        RequireCallerOwnedSerializableTransaction();

        var registration = await _dbContext.EventRegistrations
            .FirstOrDefaultAsync(
                r => r.Id == registrationId && r.UserId == expectedOwnerUserId,
                cancellationToken);

        if (registration is null)
        {
            return new EventRegistrationTransitionResult(
                Changed: false,
                ParentIntentId: null,
                PreviousStatus: null,
                FinalStatus: null,
                TransitionReason: EventRegistrationTransitionReason.NoChange,
                OccurrenceId: occurrenceId,
                OccurredAt: occurredAt,
                ActorProvenance: actorProvenance,
                ActorUserId: actorUserId,
                ChildTransitions: []);
        }

        var previousChildStatus = registration.ApprovalStatusId;
        var shouldReleaseCapacity = RegistrationApprovalStatusRules.IsCapacityBearing(
            registration.ApprovalStatusId);
        var previousParentStatus = registration.EventRegistrationIntentId.HasValue
            ? await _dbContext.EventRegistrationIntents
                .AsNoTracking()
                .Where(parent => parent.Id == registration.EventRegistrationIntentId.Value)
                .Select(parent => parent.ApprovalStatusId)
                .SingleOrDefaultAsync(cancellationToken)
            : previousChildStatus;

        registration.ApprovalStatusId = (int)ApprovalStatusEnum.Cancelled;
        _dbContext.Entry(registration).State = EntityState.Deleted;

        var parent = await RecomputeParentApprovalStatusAsync(
            registration,
            includeCurrentRegistration: false,
            noLiveChildStatusId: (int)ApprovalStatusEnum.Cancelled,
            cancellationToken: cancellationToken);
        if (parent is not null
            && !RegistrationApprovalStatusRules.IsLiveForLocationDisclosure(parent.ApprovalStatusId))
        {
            _dbContext.Entry(parent).State = EntityState.Deleted;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (shouldReleaseCapacity)
        {
            await ReleaseSessionCapacityAsync(
                registration.TenantId,
                registration.EventId,
                registration.EventSessionId,
                cancellationToken);
        }

        return new EventRegistrationTransitionResult(
            Changed: true,
            ParentIntentId: registration.EventRegistrationIntentId,
            PreviousStatus: previousParentStatus,
            FinalStatus: parent?.ApprovalStatusId ?? (int)ApprovalStatusEnum.Cancelled,
            TransitionReason: actorProvenance == EventRegistrationActorProvenance.Attendee
                ? EventRegistrationTransitionReason.SelfCancelled
                : EventRegistrationTransitionReason.Revoked,
            OccurrenceId: occurrenceId,
            OccurredAt: occurredAt,
            ActorProvenance: actorProvenance,
            ActorUserId: actorUserId,
            ChildTransitions:
            [
                new EventRegistrationChildTransition(
                    registration.Id,
                    registration.EventSessionId,
                    previousChildStatus,
                    (int)ApprovalStatusEnum.Cancelled)
            ]);
    }

    private void RequireCallerOwnedSerializableTransaction()
    {
        if (_dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory"
            && _dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "Capacity-aware registration writes require a caller-owned serializable transaction.");
        }
    }

    private async Task<EventRegistrationIntent?> RecomputeParentApprovalStatusAsync(
        EventRegistration registration,
        bool includeCurrentRegistration,
        int? noLiveChildStatusId,
        CancellationToken cancellationToken)
    {
        if (registration.EventRegistrationIntentId is not { } intentId)
        {
            return null;
        }

        var childApprovalStatusIds = await _dbContext.EventRegistrations
            .AsNoTracking()
            .Where(
                child => child.EventRegistrationIntentId == intentId
                    && child.Id != registration.Id)
            .Select(child => child.ApprovalStatusId)
            .ToListAsync(cancellationToken);

        if (includeCurrentRegistration)
        {
            childApprovalStatusIds.Add(registration.ApprovalStatusId);
        }

        var intent = await _dbContext.EventRegistrationIntents
            .FirstOrDefaultAsync(parent => parent.Id == intentId, cancellationToken);
        if (intent is null)
        {
            return null;
        }

        intent.ApprovalStatusId = RegistrationApprovalStatusRules.ResolveParentApprovalStatus(
            childApprovalStatusIds,
            noLiveChildStatusId);
        return intent;
    }

    private async Task AdjustInMemoryCapacityAsync(
        EventRegistration registration,
        bool releaseOriginalCapacity,
        bool reserveDesiredCapacity,
        Guid originalSessionId,
        Guid desiredSessionId,
        CancellationToken cancellationToken)
    {
        if (releaseOriginalCapacity)
        {
            var originalSession = await _dbContext.EventSessions
                .FirstOrDefaultAsync(session => session.Id == originalSessionId, cancellationToken);
            if (originalSession is not null)
            {
                originalSession.CurrentAudienceAttendees = Math.Max(
                    (originalSession.CurrentAudienceAttendees ?? 0) - 1,
                    0);
            }
        }

        if (reserveDesiredCapacity)
        {
            var desiredSession = await _dbContext.EventSessions
                .FirstOrDefaultAsync(session => session.Id == desiredSessionId, cancellationToken);
            if (desiredSession is not null
                && (!desiredSession.MaxAudienceAttendees.HasValue
                    || (desiredSession.CurrentAudienceAttendees ?? 0) < desiredSession.MaxAudienceAttendees.Value))
            {
                desiredSession.CurrentAudienceAttendees = (desiredSession.CurrentAudienceAttendees ?? 0) + 1;
            }
            else
            {
                registration.ApprovalStatusId = (int)ApprovalStatusEnum.Waitlisted;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ReleaseSessionCapacityAsync(
        Guid tenantId,
        Guid eventId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE event_sessions
            SET current_audience_attendees = GREATEST(COALESCE(current_audience_attendees, 0) - 1, 0)
            WHERE id = {sessionId}
              AND tenant_id = {tenantId}
              AND event_id = {eventId}
              AND is_deleted = false
            """, cancellationToken);
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

}

// ABOUTME: EF Core repository for event registration reads, capacity-aware updates, and cancellation writes.
// ABOUTME: Keeps status/session capacity transitions and cancellation atomic under Npgsql retry execution strategies.

using System.Data;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Services.Registration;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class EventRegistrationRepository : GenericRepository<EventRegistration, Guid>, IEventRegistrationRepository
{
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

    public async Task UpdateAndAdjustCapacityAsync(
        EventRegistration registration,
        CancellationToken cancellationToken)
    {
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

        var releaseOriginalCapacity = RegistrationApprovalStatusRules.IsCapacityBearing(originalApprovalStatusId)
            && (!RegistrationApprovalStatusRules.IsCapacityBearing(desiredApprovalStatusId)
                || originalSessionId != desiredSessionId);
        var reserveDesiredCapacity = RegistrationApprovalStatusRules.IsCapacityBearing(desiredApprovalStatusId)
            && (!RegistrationApprovalStatusRules.IsCapacityBearing(originalApprovalStatusId)
                || originalSessionId != desiredSessionId);
        var approvalStatusChanged = originalApprovalStatusId != desiredApprovalStatusId;
        var recomputeParentApprovalStatus = approvalStatusChanged || reserveDesiredCapacity;

        if (!releaseOriginalCapacity && !reserveDesiredCapacity && !recomputeParentApprovalStatus)
        {
            entry.State = EntityState.Modified;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        if (_dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            entry.State = EntityState.Modified;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await AdjustInMemoryCapacityAsync(
                registration,
                releaseOriginalCapacity,
                reserveDesiredCapacity,
                originalSessionId,
                desiredSessionId,
                cancellationToken);

            if (recomputeParentApprovalStatus)
            {
                await RecomputeParentApprovalStatusAsync(
                    registration,
                    includeCurrentRegistration: true,
                    noLiveChildStatusId: registration.ApprovalStatusId,
                    cancellationToken: cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            entry.CurrentValues.SetValues(desiredValues);
            entry.OriginalValues.SetValues(originalValues);
            entry.State = EntityState.Modified;

            await using var tx = await _dbContext.Database
                .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);

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
                    registration.ApprovalStatusId = (int)ApprovalStatusEnum.Waitlisted;
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                if (recomputeParentApprovalStatus)
                {
                    await RecomputeParentApprovalStatusAsync(
                        registration,
                        includeCurrentRegistration: true,
                        noLiveChildStatusId: registration.ApprovalStatusId,
                        cancellationToken: cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(CancellationToken.None);
                throw;
            }
        });
    }

    public async Task<bool> CancelAndReleaseCapacityAsync(
        Guid registrationId,
        Guid expectedOwnerUserId,
        CancellationToken cancellationToken)
    {
        if (_dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            return await CancelAndReleaseCapacityCoreAsync(
                registrationId,
                expectedOwnerUserId,
                cancellationToken);
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _dbContext.Database
                .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            try
            {
                var result = await CancelAndReleaseCapacityCoreAsync(
                    registrationId,
                    expectedOwnerUserId,
                    cancellationToken);
                await tx.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await tx.RollbackAsync(CancellationToken.None);
                throw;
            }
        });
    }

    private async Task<bool> CancelAndReleaseCapacityCoreAsync(
        Guid registrationId,
        Guid expectedOwnerUserId,
        CancellationToken cancellationToken)
    {
        var registration = await _dbContext.EventRegistrations
            .FirstOrDefaultAsync(
                r => r.Id == registrationId && r.UserId == expectedOwnerUserId,
                cancellationToken);

        if (registration is null)
        {
            return false;
        }

        var shouldReleaseCapacity = RegistrationApprovalStatusRules.IsCapacityBearing(
            registration.ApprovalStatusId);

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

        return true;
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

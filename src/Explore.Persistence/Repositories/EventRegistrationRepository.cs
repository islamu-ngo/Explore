// ABOUTME: EF Core repository for event registration reads and cancellation writes.
// ABOUTME: Keeps cancellation and capacity release atomic under Npgsql retry execution strategies.

using System.Data;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
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

    public async Task<bool> CancelAndReleaseCapacityAsync(Guid registrationId, CancellationToken cancellationToken)
    {
        if (_dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            return await CancelAndReleaseCapacityCoreAsync(registrationId, cancellationToken);
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _dbContext.Database
                .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            try
            {
                var result = await CancelAndReleaseCapacityCoreAsync(registrationId, cancellationToken);
                await tx.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    private async Task<bool> CancelAndReleaseCapacityCoreAsync(Guid registrationId, CancellationToken cancellationToken)
    {
        var registration = await _dbContext.EventRegistrations
            .FirstOrDefaultAsync(r => r.Id == registrationId, cancellationToken);

        if (registration is null)
        {
            return false;
        }

        var shouldReleaseCapacity = registration.ApprovalStatusId == (int)ApprovalStatusEnum.Approved;

        _dbContext.Entry(registration).State = EntityState.Deleted;
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (shouldReleaseCapacity)
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE event_sessions
                SET current_audience_attendees = GREATEST(COALESCE(current_audience_attendees, 0) - 1, 0)
                WHERE id = {registration.EventSessionId}
                  AND is_deleted = false
                """, cancellationToken);
        }

        return true;
    }
}

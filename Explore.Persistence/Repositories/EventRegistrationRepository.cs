using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class EventRegistrationRepository : GenericRepository<EventRegistration, Guid>, IEventRegistrationRepository
{
    private readonly ExploreDbContext _dbContext;

    public EventRegistrationRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EventRegistration?> GetByIdWithDetails(Guid id)
    {
        return await _dbContext.EventRegistrations
            .AsNoTracking()
            .Include(r => r.User)
                .ThenInclude(u => u!.Pii)
            .Include(r => r.EventSession)
                .ThenInclude(s => s.Event)
            .Include(r => r.ApprovalStatus)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<EventRegistration?> GetRegistrationByUserAndSession(Guid userId, Guid eventSessionId)
    {
        return await _dbContext.EventRegistrations
            .AsNoTracking()
            .Include(r => r.EventSession)
                .ThenInclude(s => s.Event)
            .Include(r => r.ApprovalStatus)
            .FirstOrDefaultAsync(r => r.UserId == userId && r.EventSessionId == eventSessionId);
    }

    public async Task<List<EventRegistration>> GetRegistrationsBySession(Guid eventSessionId)
    {
        return await _dbContext.EventRegistrations
            .AsNoTracking()
            .Include(r => r.User)
                .ThenInclude(u => u!.Pii)
            .Include(r => r.EventSession)
                .ThenInclude(s => s.Event)
            .Include(r => r.ApprovalStatus)
            .Where(r => r.EventSessionId == eventSessionId)
            .ToListAsync();
    }

    public async Task<List<EventRegistration>> GetRegistrationsByUser(Guid userId)
    {
        return await _dbContext.EventRegistrations
            .AsNoTracking()
            .Include(r => r.EventSession)
                .ThenInclude(s => s.Event)
            .Include(r => r.ApprovalStatus)
            .Where(r => r.UserId == userId)
            .ToListAsync();
    }

    public async Task<bool> IsUserRegisteredForSession(Guid userId, Guid eventSessionId)
    {
        return await _dbContext.EventRegistrations
            .AsNoTracking()
            .AnyAsync(r => r.UserId == userId && r.EventSessionId == eventSessionId);
    }

    public async Task<(List<EventRegistration> Items, int TotalCount)> GetRegistrationsWithDetailsPaged(int pageNumber, int pageSize)
    {
        var query = _dbContext.EventRegistrations
            .AsNoTracking()
            .AsSplitQuery()
            .Include(r => r.User)
                .ThenInclude(u => u!.Pii)
            .Include(r => r.EventSession)
                .ThenInclude(s => s.Event)
            .Include(r => r.ApprovalStatus)
            .OrderByDescending(r => r.Id);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}

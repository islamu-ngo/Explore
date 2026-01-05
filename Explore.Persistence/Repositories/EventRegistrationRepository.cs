using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class EventRegistrationRepository : GenericRepository<EventRegistration, Guid>, IEventRegistrationRepository
    {
        private readonly ExploreDbContext _dbContext;

        public EventRegistrationRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<EventRegistration?> GetRegistrationByUserAndSession(Guid userId, Guid eventSessionId)
        {
            return await _dbContext.EventRegistrations
                .Include(r => r.ApprovalStatus)
                .FirstOrDefaultAsync(r => r.UserId == userId && r.EventSessionId == eventSessionId);
        }

        public async Task<List<EventRegistration>> GetRegistrationsBySession(Guid eventSessionId)
        {
            return await _dbContext.EventRegistrations
                .Include(r => r.User)
                .Include(r => r.ApprovalStatus)
                .Where(r => r.EventSessionId == eventSessionId)
                .ToListAsync();
        }

        public async Task<List<EventRegistration>> GetRegistrationsByUser(Guid userId)
        {
            return await _dbContext.EventRegistrations
                .Include(r => r.EventSession)
                    .ThenInclude(s => s.Event)
                .Include(r => r.ApprovalStatus)
                .Where(r => r.UserId == userId)
                .ToListAsync();
        }

        public async Task<bool> IsUserRegisteredForSession(Guid userId, Guid eventSessionId)
        {
            return await _dbContext.EventRegistrations
                .AnyAsync(r => r.UserId == userId && r.EventSessionId == eventSessionId);
        }
    }
}

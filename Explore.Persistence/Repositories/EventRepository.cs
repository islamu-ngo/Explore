using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class EventRepository : GenericRepository<Event, Guid>, IEventRepository
    {
        private readonly ExploreDbContext _dbContext;

        public EventRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Event>> GetEventsWithDetails()
        {
            return await _dbContext.Events
                .Include(e => e.EventType)
                .Include(e => e.AudienceGender)
                .Include(e => e.AudienceAge)
                .Include(e => e.Actor)
                    .ThenInclude(a => a.ActorType)
                .Include(e => e.FeaturedImage)
                .Include(e => e.EventStatus)
                .Include(e => e.VisibilityType)
                .Include(e => e.EventFormat)
                .Include(e => e.Madhab)
                .ToListAsync();
        }

        public async Task<Event?> GetEventWithDetails(Guid id)
        {
            return await _dbContext.Events
                .Include(e => e.EventType)
                .Include(e => e.AudienceGender)
                .Include(e => e.AudienceAge)
                .Include(e => e.Actor)
                    .ThenInclude(a => a.ActorType)
                .Include(e => e.Actor)
                    .ThenInclude(a => a.ProfilePicture)
                .Include(e => e.FeaturedImage)
                .Include(e => e.EventStatus)
                .Include(e => e.VisibilityType)
                .Include(e => e.EventFormat)
                .Include(e => e.Madhab)
                .Include(e => e.AtprotoRecord)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<List<Event>> GetMyEventsWithDetails(string userId)
        {
            Guid userGuid;
            bool isGuid = Guid.TryParse(userId, out userGuid);

            var query = _dbContext.Events
                .Include(e => e.EventType)
                .Include(e => e.AudienceGender)
                .Include(e => e.AudienceAge)
                .Include(e => e.Actor)
                    .ThenInclude(a => a.ActorType)
                .Include(e => e.FeaturedImage)
                .Include(e => e.EventStatus)
                .Include(e => e.VisibilityType)
                .Include(e => e.EventFormat)
                .Include(e => e.Madhab)
                .AsQueryable();

            if (isGuid)
            {
                query = query.Where(e =>
                    _dbContext.Users.Any(u => u.Id == userGuid && u.ActorId == e.ActorId) ||
                    _dbContext.OrganizationMembers.Any(om =>
                        om.UserId == userGuid &&
                        _dbContext.Organizations.Any(o => o.Id == om.OrganizationId && o.ActorId == e.ActorId)));
            }

            return await query.ToListAsync();
        }
    }
}

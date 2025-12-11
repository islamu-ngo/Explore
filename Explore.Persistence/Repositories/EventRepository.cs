using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
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

        public async Task<List<EventListDto>> GetEventsWithDetails()
        {
            var events = await _dbContext.Events
                .Include(e => e.EventType)
                .Include(e => e.ProgramType)
                .Include(e => e.AudienceGender)
                .Include(e => e.AudienceAge)
                .Include(e => e.Organization)
                .Include(e => e.FeaturedImage)
                .Select(p => new EventListDto()
                {
                    Id = p.Id,
                    ProgramTypeId = p.ProgramTypeId,
                    ProgramTypeFullName = p.ProgramType.FullName,
                    Title = p.Title,
                    Description = p.Description,
                    AudienceGenderId = p.AudienceGenderId,
                    AudienceGenderFullName = p.AudienceGender.FullName,
                    AudienceAgeId = p.AudienceAgeId,
                    AudienceAgeFullName = p.AudienceAge.FullName,
                    AudienceAgeMinAge = p.AudienceAge.MinAge,
                    AudienceAgeMaxAge = p.AudienceAge.MaxAge,
                    OrganizationId = p.OrganizationId,
                    OrganizationFullName = p.Organization.FullName,
                    AudienceAttendees = p.AudienceAttendees,
                    Price = p.Price,
                    FeaturedImageId = p.FeaturedImageId,
                    FeaturedImageUri = p.FeaturedImage.Uri,
                    IsRegistrationRequired = p.IsRegistrationRequired,
                    TotalViews = p.TotalViews,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    Country = p.Country,
                    City = p.City,
                    PostCode = p.PostCode,
                    Address = p.Address,
                    ProgramUrl = p.ProgramUrl,
                    EventTypeId = p.EventTypeId,
                    EventTypeFullName = p.EventType.FullName
                })
                .ToListAsync();
            return events;
        }

        public async Task<EventDto> GetEventWithDetails(Guid id)
        {
            var eventEntity = await _dbContext.Events
                .Include(e => e.EventType)
                .Include(e => e.ProgramType)
                .Include(e => e.AudienceGender)
                .Include(e => e.AudienceAge)
                .Include(e => e.Organization)
                .Include(e => e.FeaturedImage)
                .Select(p => new EventDto()
                {
                    Id = p.Id,
                    ProgramTypeId = p.ProgramTypeId,
                    ProgramTypeFullName = p.ProgramType.FullName,
                    Title = p.Title,
                    Description = p.Description,
                    AudienceGenderId = p.AudienceGenderId,
                    AudienceGenderFullName = p.AudienceGender.FullName,
                    AudienceAgeId = p.AudienceAgeId,
                    AudienceAgeFullName = p.AudienceAge.FullName,
                    AudienceAgeMinAge = p.AudienceAge.MinAge,
                    AudienceAgeMaxAge = p.AudienceAge.MaxAge,
                    OrganizationId = p.OrganizationId,
                    OrganizationFullName = p.Organization.FullName,
                    AudienceAttendees = p.AudienceAttendees,
                    Price = p.Price,
                    FeaturedImageId = p.FeaturedImageId,
                    FeaturedImageUri = p.FeaturedImage.Uri,
                    IsRegistrationRequired = p.IsRegistrationRequired,
                    TotalViews = p.TotalViews,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    Country = p.Country,
                    City = p.City,
                    PostCode = p.PostCode,
                    Address = p.Address,
                    ProgramUrl = p.ProgramUrl,
                    EventTypeId = p.EventTypeId,
                    EventTypeFullName = p.EventType.FullName
                })
                .FirstOrDefaultAsync(e => e.Id == id);
            return eventEntity;
        }

        public async Task<List<EventListDto>> GetMyEventsWithDetails(string userId)
        {
            Guid userGuid;
            bool isGuid = Guid.TryParse(userId, out userGuid);

            var query = _dbContext.Events
                .Include(e => e.EventType)
                .Include(e => e.ProgramType)
                .Include(e => e.AudienceGender)
                .Include(e => e.AudienceAge)
                .Include(e => e.Organization)
                .ThenInclude(o => o.Members)
                .Include(e => e.FeaturedImage)
                .AsQueryable();

            if (isGuid)
            {
                query = query.Where(e => e.Organization.CreatedByUserId == userId || e.Organization.Members.Any(m => m.UserId == userGuid));
            }
            else
            {
                query = query.Where(e => e.Organization.CreatedByUserId == userId);
            }

            var events = await query
                .Select(p => new EventListDto()
                {
                    Id = p.Id,
                    ProgramTypeId = p.ProgramTypeId,
                    ProgramTypeFullName = p.ProgramType.FullName,
                    Title = p.Title,
                    Description = p.Description,
                    AudienceGenderId = p.AudienceGenderId,
                    AudienceGenderFullName = p.AudienceGender.FullName,
                    AudienceAgeId = p.AudienceAgeId,
                    AudienceAgeFullName = p.AudienceAge.FullName,
                    AudienceAgeMinAge = p.AudienceAge.MinAge,
                    AudienceAgeMaxAge = p.AudienceAge.MaxAge,
                    OrganizationId = p.OrganizationId,
                    OrganizationFullName = p.Organization.FullName,
                    AudienceAttendees = p.AudienceAttendees,
                    Price = p.Price,
                    FeaturedImageId = p.FeaturedImageId,
                    FeaturedImageUri = p.FeaturedImage.Uri,
                    IsRegistrationRequired = p.IsRegistrationRequired,
                    TotalViews = p.TotalViews,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    Country = p.Country,
                    City = p.City,
                    PostCode = p.PostCode,
                    Address = p.Address,
                    ProgramUrl = p.ProgramUrl,
                    EventTypeId = p.EventTypeId,
                    EventTypeFullName = p.EventType.FullName
                })
                .ToListAsync();
            return events;
        }
    }
}

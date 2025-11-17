using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Education;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.Program;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Persistence.Repositories
{
    public class ProgramRepository : GenericRepository<Program, Guid>, IProgramRepository
    {
        private readonly ExploreDbContext _dbContext;
        public ProgramRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<List<ProgramListDto>> GetProgramsWithDetails()
        {
            var programs = await _dbContext.Programs
                .Include(e => e.ProgramType)
                .Include(e => e.AudienceGender)
                .Include(e => e.AudienceAge)
                .Select(p => new ProgramListDto()
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
                    TotalViews = p.TotalViews,
                    Country = p.Country,
                    City = p.City,
                    PostCode = p.PostCode,
                    Address = p.Address,
                    ProgramUrl = p.ProgramUrl
                })
                .ToListAsync();
            return programs;
        }
        public async Task<ProgramDto> GetProgramWithDetails(Guid id)
        {
            var program = await _dbContext.Programs
                .Include(e => e.ProgramType)
                .Include(e => e.AudienceGender)
                .Include(e => e.AudienceAge)
                .Select(p => new ProgramDto
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
                    Country = p.Country,
                    City = p.City,
                    PostCode = p.PostCode,
                    Address = p.Address,
                    ProgramUrl = p.ProgramUrl,

                    // ✅ Projection conditionnelle : seulement les données pertinentes
                    Event = p is Event
                        ? new EventSpecificDto
                        {
                            EventTypeId = ((Event)p).EventTypeId,
                            EventTypeFullName = ((Event)p).EventType.FullName
                        }
                        : null,

                    Education = p is Education
                        ? new EducationSpecificDto
                        {
                            EducationTypeId = ((Education)p).EducationTypeId,
                            EducationTypeFullName = ((Education)p).EducationType.FullName
                        }
                        : null
                })
                .FirstOrDefaultAsync(p => p.Id == id);
            return program;
        }

        public async Task<List<ProgramListDto>> GetByOrganization(Guid organizationId)
        {
            var programs = await _dbContext.Programs
                .Where(p => p.OrganizationId == organizationId)
                .Include(e => e.ProgramType)
                .Include(e => e.AudienceGender)
                .Include(e => e.AudienceAge)
                .Select(p => new ProgramListDto()
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
                    TotalViews = p.TotalViews,
                    Country = p.Country,
                    City = p.City,
                    PostCode = p.PostCode,
                    Address = p.Address,
                    ProgramUrl = p.ProgramUrl
                })
                .ToListAsync();
            return programs;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class EducationRepository : GenericRepository<Education, Guid>, IEducationRepository
    {
        private readonly ExploreDbContext _dbContext;
        public EducationRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<List<Education>> GetEducationsWithDetails()
        {
            var educations = await _dbContext.Educations
                .Include(e => e.EducationType)
                .Include(e => e.ProgramType)
                .Include(e => e.AudienceGender)
                .Include(e => e.AudienceAge)
                .ToListAsync();
            return educations;
        }
        public async Task<Education> GetEducationWithDetails(Guid id)
        {
            var education = await _dbContext.Educations
                .Include(e => e.EducationType)
                .Include(e => e.ProgramType)
                .Include(e => e.AudienceGender)
                .Include(e => e.AudienceAge)
                .FirstOrDefaultAsync(e => e.Id == id);
            return education;
        }
    }
}

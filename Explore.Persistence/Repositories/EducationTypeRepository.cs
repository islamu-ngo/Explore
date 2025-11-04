using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class EducationTypeRepository : GenericRepository<EducationType, int>, IEducationTypeRepository
    {
        private readonly ExploreDbContext _dbContext;
        public EducationTypeRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<EducationType>> GetEducationTypesWithDetails()
        {
            var educationTypes = await _dbContext.EducationTypes
                .ToListAsync();
            return educationTypes;
        }

        public async Task<EducationType> GetEducationTypeWithDetails(int id)
        {
            var educationType = await _dbContext.EducationTypes
                .FirstOrDefaultAsync(e => e.Id == id);
            return educationType;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class ProgramTypeRepository : GenericRepository<ProgramType, int>, IProgramTypeRepository
    {
        private readonly ExploreDbContext _dbContext;
        public ProgramTypeRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<ProgramType>> GetProgramTypesWithDetails()
        {
            var programTypes = await _dbContext.ProgramTypes
                .ToListAsync();
            return programTypes;
        }

        public async Task<ProgramType> GetProgramTypeWithDetails(int id)
        {
            var programType = await _dbContext.ProgramTypes
                .FirstOrDefaultAsync(p => p.Id == id);
            return programType;
        }
    }
}

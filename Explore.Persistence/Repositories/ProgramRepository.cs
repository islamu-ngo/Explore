using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class ProgramRepository : GenericRepository<Program, Guid>, IProgramRepository
    {
        private readonly ExploreDbContext _dbContext;
        public ProgramRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<List<Program>> GetProgramsWithDetails()
        {
            var programs = await _dbContext.Programs
                .Include(e => e.ProgramType)
                .Include(e => e.AudienceGender)
                .Include(e => e.AudienceAge)
                .ToListAsync();
            return programs;
        }
        public async Task<Program> GetProgramWithDetails(Guid id)
        {
            var program = await _dbContext.Programs
                .Include(e => e.ProgramType)
                .Include(e => e.AudienceGender)
                .Include(e => e.AudienceAge)
                .FirstOrDefaultAsync(p => p.Id == id);
            return program;
        }
    }
}

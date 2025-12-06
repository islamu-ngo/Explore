using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class ProgramRegistrationRepository : GenericRepository<ProgramRegistartion, Guid>, IProgramRegistrationRepository
    {
        private readonly ExploreDbContext _dbContext;
        public ProgramRegistrationRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<ProgramRegistartion>> GetProgramRegistrationsWithDetails()
        {
            var programRegistrations = await _dbContext.ProgramRegistartions
                .Include(pr => pr.Program)
                .Include(pr => pr.StatusType)
                .ToListAsync();
            return programRegistrations;
        }

        public async Task<ProgramRegistartion> GetProgramRegistrationWithDetails(Guid id)
        {
            var programRegistration = await _dbContext.ProgramRegistartions
                .Include(pr => pr.Program)
                .Include(pr => pr.StatusType)
                .FirstOrDefaultAsync(pr => pr.Id == id);
            return programRegistration;
        }

        public async Task<bool> IsUserAlreadyRegisteredAsync(Guid userId, Guid programId)
        {
            return await _dbContext.ProgramRegistartions
                .AnyAsync(pr => pr.UserId == userId && pr.ProgramId == programId);
        }

        public async Task<List<ProgramRegistartion>> GetRegistrationsForProgramAsync(Guid programId)
        {
            return await _dbContext.ProgramRegistartions
                .Include(pr => pr.StatusType)
                .Where(pr => pr.ProgramId == programId)
                .ToListAsync();
        }
    }
}

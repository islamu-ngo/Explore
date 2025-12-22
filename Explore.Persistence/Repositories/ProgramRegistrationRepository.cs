using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class ProgramRegistrationRepository : GenericRepository<ProgramRegistration, Guid>, IProgramRegistrationRepository
    {
        private readonly ExploreDbContext _dbContext;
        public ProgramRegistrationRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<ProgramRegistration>> GetProgramRegistrationsWithDetails()
        {
            var programRegistrations = await _dbContext.ProgramRegistartions
                .Include(pr => pr.Program)
                .Include(pr => pr.StatusType)
                .ToListAsync();
            return programRegistrations;
        }

        public async Task<ProgramRegistration> GetProgramRegistrationWithDetails(Guid id)
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
                .Include(pr => pr.Program)
                .Where(pr => pr.ProgramId == programId)
                .ToListAsync();
        }

        public async Task<List<ProgramRegistartion>> GetRegistrationsForUserAsync(Guid userId)
        {
            return await _dbContext.ProgramRegistartions
                .Include(pr => pr.StatusType)
                .Include(pr => pr.Program)
                    .ThenInclude(p => p.Organization)
                .Where(pr => pr.UserId == userId)
                .ToListAsync();
        }
    }
}

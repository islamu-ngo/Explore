using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class EventRegistrationRepository : GenericRepository<EventRegistration, Guid>, IProgramRegistrationRepository
    {
        private readonly ExploreDbContext _dbContext;
        public EventRegistrationRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<EventRegistration>> GetProgramRegistrationsWithDetails()
        {
            var programRegistrations = await _dbContext.EventRegistrations
                .Include(pr => pr.Event)
                .Include(pr => pr.ApprovalStatus)
                .ToListAsync();
            return programRegistrations;
        }

        public async Task<EventRegistration> GetProgramRegistrationWithDetails(Guid id)
        {
            var programRegistration = await _dbContext.EventRegistartions
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

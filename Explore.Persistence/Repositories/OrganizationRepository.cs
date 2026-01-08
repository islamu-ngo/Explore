using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class OrganizationRepository : GenericRepository<Organization, Guid>, IOrganizationRepository
    {
        private readonly ExploreDbContext _dbContext;

        public OrganizationRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Organization>> GetOrganizationsWithDetails()
        {
            return await _dbContext.Organizations
                .Include(o => o.ApprovalStatus)
                .Include(o => o.Actor)
                .Include(o => o.Tenant)
                .ToListAsync();
        }

        public async Task<Organization?> GetOrganizationWithDetails(Guid id)
        {
            return await _dbContext.Organizations
                .Include(o => o.ApprovalStatus)
                .Include(o => o.Actor)
                .Include(o => o.Tenant)
                .Include(o => o.Members)
                    .ThenInclude(m => m.User)
                .Include(o => o.Members)
                    .ThenInclude(m => m.OrganizationRole)
                .Include(o => o.Members)
                    .ThenInclude(m => m.OrganizationPosition)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<List<Organization>> GetMyOrganizations(Guid userId)
        {
            return await _dbContext.Organizations
                .Include(o => o.ApprovalStatus)
                .Include(o => o.Actor)
                .Include(o => o.Members)
                    .ThenInclude(m => m.OrganizationRole)
                .Where(o => o.Members.Any(m => m.UserId == userId))
                .ToListAsync();
        }
    }
}

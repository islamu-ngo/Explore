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
            var organizations = await _dbContext.Organizations
                .Include(o => o.StatusType)
                .ToListAsync();
            return organizations;
        }

        public async Task<Organization> GetOrganizationWithDetails(Guid id)
        {
            var organization = await _dbContext.Organizations
                .Include(o => o.StatusType)
                .FirstOrDefaultAsync(o => o.Id == id);
            return organization;
        }
    }
}

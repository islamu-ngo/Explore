using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class StatusTypeRepository : GenericRepository<StatusType, int>, IStatusTypeRepository
    {
        private readonly ExploreDbContext _dbContext;
        public StatusTypeRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<List<StatusType>> GetStatusTypesWithDetails()
        {
            var statusTypes = await _dbContext.StatusTypes
                .ToListAsync();
            return statusTypes;
        }
        public async Task<StatusType> GetStatusTypeWithDetails(int id)
        {
            var statusType = await _dbContext.StatusTypes
                .FirstOrDefaultAsync(s => s.Id == id);
            return statusType;
        }
    }
}

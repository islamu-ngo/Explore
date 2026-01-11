using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class TenantSettingsRepository : GenericRepository<TenantSettings, Guid>, ITenantSettingsRepository
    {
        private readonly ExploreDbContext _dbContext;

        public TenantSettingsRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<TenantSettings?> GetByTenant(Guid tenantId)
        {
            return await _dbContext.TenantSettings
                .FirstOrDefaultAsync(ts => ts.TenantId == tenantId);
        }

        public async Task<TenantSettings?> GetTenantSettingsWithDetails(Guid id)
        {
            return await _dbContext.TenantSettings
                .Include(ts => ts.Tenant)
                .FirstOrDefaultAsync(ts => ts.Id == id);
        }

        public async Task<List<TenantSettings>> GetTenantSettingsListWithDetails()
        {
            return await _dbContext.TenantSettings
                .Include(ts => ts.Tenant)
                .ToListAsync();
        }
    }
}

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class TenantSettingsRepository : GenericRepository<TenantSettings, int>, ITenantSettingsRepository
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
    }
}

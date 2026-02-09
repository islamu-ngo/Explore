using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class TenantRepository : GenericRepository<Tenant, Guid>, ITenantRepository
{
    private readonly ExploreDbContext _dbContext;

    public TenantRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Tenant?> GetTenantBySlug(string slug)
    {
        return await _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == slug);
    }
}

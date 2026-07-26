// ABOUTME: Repository implementation for linked external identity-provider login records.
// ABOUTME: Resolves provider subjects to global users before tenant authorization is evaluated.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class UserExternalLoginRepository : GenericRepository<UserExternalLogin, Guid>, IUserExternalLoginRepository
{
    private readonly ExploreDbContext _dbContext;

    public UserExternalLoginRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserExternalLogin?> GetByProviderAndKey(string provider, string providerKey)
    {
        return await _dbContext.UserExternalLogins
            .IgnoreTenantFilter(TenantFilterBypassReasons.UserExternalLoginAuthentication)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderKey == providerKey);
    }

    public async Task<List<UserExternalLogin>> GetByUser(Guid userId)
    {
        return await _dbContext.UserExternalLogins
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .ToListAsync();
    }

}

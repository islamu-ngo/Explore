using Explore.Application.Contracts.Persistence;
using Explore.Domain;
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

    public async Task<UserExternalLogin?> GetUserExternalLoginWithDetails(Guid id)
    {
        return await _dbContext.UserExternalLogins
            .AsNoTracking()
            .Include(l => l.User)
                .ThenInclude(u => u!.Pii)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<List<UserExternalLogin>> GetUserExternalLoginsWithDetails()
    {
        return await _dbContext.UserExternalLogins
            .AsNoTracking()
            .Include(l => l.User)
                .ThenInclude(u => u!.Pii)
            .ToListAsync();
    }
}

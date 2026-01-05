using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class UserAuthenticationTokenRepository : GenericRepository<UserAuthenticationToken, Guid>, IUserAuthenticationTokenRepository
    {
        private readonly ExploreDbContext _dbContext;

        public UserAuthenticationTokenRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UserAuthenticationToken?> GetByUserAndProvider(Guid userId, string provider)
        {
            return await _dbContext.UserAuthenticationTokens
                .FirstOrDefaultAsync(t => t.UserId == userId && t.Provider == provider);
        }

        public async Task<List<UserAuthenticationToken>> GetByUser(Guid userId)
        {
            return await _dbContext.UserAuthenticationTokens
                .Where(t => t.UserId == userId)
                .ToListAsync();
        }
    }
}

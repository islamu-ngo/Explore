using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class UserRepository : GenericRepository<User, Guid>, IUserRepository
    {
        private readonly ExploreDbContext _dbContext;

        public UserRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<User?> GetUserWithDetails(Guid id)
        {
            return await _dbContext.Users
                .Include(u => u.Actor)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<List<User>> GetUsersByIdsAsync(List<Guid> ids)
        {
            return await _dbContext.Users
                .Where(u => ids.Contains(u.Id))
                .ToListAsync();
        }

        public async Task<bool> ExistsByEmail(string email)
        {
            return await _dbContext.Users
                .AnyAsync(u => u.Email == email);
        }
    }
}

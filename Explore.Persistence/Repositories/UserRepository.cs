using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class UserRepository : GenericRepository<User, Guid>, IUserRepository
{
    private static readonly Func<ExploreDbContext, Guid, Task<User?>> GetByIdCompiled =
        EF.CompileAsyncQuery((ExploreDbContext ctx, Guid id) =>
            ctx.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == id));

    private readonly ExploreDbContext _dbContext;

    public UserRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public new async Task<User?> GetById(Guid id)
    {
        return await GetByIdCompiled(_dbContext, id);
    }

    public async Task<User?> GetUserWithDetails(Guid id)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.Actor)
                .ThenInclude(a => a.ProfilePicture)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetUserByEmail(string email)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<List<User>> GetUsersByIdsAsync(List<Guid> ids)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToListAsync();
    }

    public async Task<bool> ExistsByEmail(string email)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == email);
    }
}

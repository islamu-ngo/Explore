// ABOUTME: EF Core repository for external authentication token records.
// ABOUTME: Provides user-scoped queries to keep credential metadata self-service only.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class UserAuthenticationTokenRepository : GenericRepository<UserAuthenticationToken, Guid>, IUserAuthenticationTokenRepository
{
    private readonly ExploreDbContext _dbContext;

    public UserAuthenticationTokenRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserAuthenticationToken?> GetByUserAndProvider(
        Guid userId,
        string provider,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserAuthenticationTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Provider == provider, cancellationToken);
    }

    public async Task<List<UserAuthenticationToken>> GetByUser(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserAuthenticationTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserAuthenticationToken?> GetByIdForUser(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserAuthenticationTokens
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);
    }

    public async Task<UserAuthenticationToken?> GetUserAuthenticationTokenWithDetailsForUser(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserAuthenticationTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);
    }

    public async Task<List<UserAuthenticationToken>> GetUserAuthenticationTokensWithDetailsForUser(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserAuthenticationTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .ToListAsync(cancellationToken);
    }
}

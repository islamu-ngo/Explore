using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class OrganizationReviewRepository : GenericRepository<OrganizationReview, Guid>, IOrganizationReviewRepository
{
    private readonly ExploreDbContext _dbContext;

    public OrganizationReviewRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<OrganizationReview>> GetByOrganizationId(Guid organizationId)
    {
        return await _dbContext.Set<OrganizationReview>()
            .AsNoTracking()
            .Include(r => r.Organization)
                .ThenInclude(o => o!.Pii)
            .Include(r => r.User)
                .ThenInclude(u => u!.Pii)
            .Where(r => r.OrganizationId == organizationId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<OrganizationReview>> GetByUserId(Guid userId)
    {
        return await _dbContext.Set<OrganizationReview>()
            .AsNoTracking()
            .Include(r => r.Event)
            .Include(r => r.Organization)
                .ThenInclude(o => o!.Pii)
            .Include(r => r.User)
                .ThenInclude(u => u!.Pii)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> HasUserReviewedProgram(Guid userId, Guid programId)
    {
        return await _dbContext.Set<OrganizationReview>()
            .AsNoTracking()
            .AnyAsync(r => r.UserId == userId && r.EventId == programId);
    }
}

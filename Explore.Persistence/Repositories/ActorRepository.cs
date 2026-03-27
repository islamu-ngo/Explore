using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class ActorRepository : GenericRepository<Actor, Guid>, IActorRepository
{
    private readonly ExploreDbContext _dbContext;

    public ActorRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public new async Task<Actor?> GetById(Guid id)
    {
        return await _dbContext.Actors
            .Include(a => a.Pii)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Actor?> GetActorWithDetails(Guid id)
    {
        return await _dbContext.Actors
            .AsNoTracking()
            .AsSplitQuery()
            .Include(a => a.Pii)
            .Include(a => a.ActorType)
            .Include(a => a.DidCustodyType)
            .Include(a => a.ProfilePicture)
            .Include(a => a.Tenant)
            .Include(a => a.User)
                .ThenInclude(u => u!.Pii)
            .Include(a => a.Organization)
                .ThenInclude(o => o!.Pii)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Actor?> GetActorByDid(string did)
    {
        return await _dbContext.Actors
            .AsNoTracking()
            .Include(a => a.Pii)
            .Include(a => a.ActorType)
            .FirstOrDefaultAsync(a => a.Pii != null && a.Pii.Did == did);
    }

    public async Task<Actor?> GetActorByHandle(string handle)
    {
        return await _dbContext.Actors
            .AsNoTracking()
            .Include(a => a.Pii)
            .Include(a => a.ActorType)
            .FirstOrDefaultAsync(a => a.Pii != null && a.Pii.Handle == handle);
    }

    public async Task<List<Actor>> GetActorsByTenant(Guid tenantId)
    {
        return await _dbContext.Actors
            .AsNoTracking()
            .Include(a => a.Pii)
            .Include(a => a.ActorType)
            .Where(a => a.TenantId == tenantId)
            .ToListAsync();
    }

    public async Task<bool> DidExists(string did)
    {
        return await _dbContext.Actors
            .AsNoTracking()
            .AnyAsync(a => a.Pii != null && a.Pii.Did == did);
    }

    public async Task<Actor?> GetActorByUserId(Guid userId)
    {
        return await _dbContext.Actors
            .AsNoTracking()
            .Include(a => a.Pii)
            .Include(a => a.ActorType)
            .FirstOrDefaultAsync(a => a.UserId == userId);
    }

    public async Task<Actor?> GetActorByOrganizationId(Guid organizationId)
    {
        return await _dbContext.Actors
            .AsNoTracking()
            .Include(a => a.Pii)
            .Include(a => a.ActorType)
            .FirstOrDefaultAsync(a => a.OrganizationId == organizationId);
    }

    public async Task<Actor?> GetActorByGroupId(Guid groupId)
    {
        return await _dbContext.Actors
            .AsNoTracking()
            .Include(a => a.Pii)
            .FirstOrDefaultAsync(a => a.GroupId == groupId);
    }

    public async Task<(List<Actor> Items, int TotalCount)> GetActorsWithDetailsPaged(int pageNumber, int pageSize)
    {
        var query = _dbContext.Actors
            .AsNoTracking()
            .AsSplitQuery()
            .Include(a => a.Pii)
            .Include(a => a.ActorType)
            .Include(a => a.DidCustodyType)
            .Include(a => a.ProfilePicture)
            .OrderByDescending(a => a.IndexedAt ?? DateTime.MinValue);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<int> ForgetPiiAsync(Guid actorId)
    {
        return await _dbContext.ActorPii
            .Where(p => p.ActorId == actorId)
            .ExecuteDeleteAsync();
    }
}

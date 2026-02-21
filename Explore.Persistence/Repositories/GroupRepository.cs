using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class GroupRepository : GenericRepository<Group, Guid>, IGroupRepository
{
    private static readonly Func<ExploreDbContext, Guid, Task<Group?>> GetByIdCompiled =
        EF.CompileAsyncQuery((ExploreDbContext ctx, Guid id) =>
            ctx.Groups
                .AsNoTracking()
                .FirstOrDefault(g => g.Id == id));

    private readonly ExploreDbContext _dbContext;

    public GroupRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public new async Task<Group?> GetById(Guid id)
    {
        return await GetByIdCompiled(_dbContext, id);
    }

    public async Task<List<Group>> GetGroupsWithDetails()
    {
        return await _dbContext.Groups
            .AsNoTracking()
            .AsSplitQuery()
            .Include(g => g.ApprovalStatus)
            .Include(g => g.Actor)
                .ThenInclude(a => a!.Pii)
            .Include(g => g.Actor)
                .ThenInclude(a => a!.ProfilePicture)
            .Include(g => g.Tenant)
            .ToListAsync();
    }

    public async Task<Group?> GetGroupWithDetails(Guid id)
    {
        return await _dbContext.Groups
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .Include(g => g.ApprovalStatus)
            .Include(g => g.Actor)
                .ThenInclude(a => a!.Pii)
            .Include(g => g.Actor)
                .ThenInclude(a => a!.ProfilePicture)
            .Include(g => g.Tenant)
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
                .ThenInclude(u => u!.Pii)
            .Include(g => g.Members)
                .ThenInclude(m => m.Role)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<List<Group>> GetMyGroups(Guid userId)
    {
        return await _dbContext.Groups
            .AsNoTracking()
            .AsSplitQuery()
            .Include(g => g.ApprovalStatus)
            .Include(g => g.Actor)
                .ThenInclude(a => a!.Pii)
            .Include(g => g.Actor)
                .ThenInclude(a => a!.ProfilePicture)
            .Include(g => g.Members)
                .ThenInclude(m => m.Role)
            .Where(g => g.Members.Any(m => m.UserId == userId))
            .ToListAsync();
    }

    public async Task<(List<Group> Items, int TotalCount)> GetGroupsWithDetailsPaged(int pageNumber, int pageSize)
    {
        var query = _dbContext.Groups
            .AsNoTracking()
            .AsSplitQuery()
            .Include(g => g.ApprovalStatus)
            .Include(g => g.Actor)
                .ThenInclude(a => a!.Pii)
            .Include(g => g.Actor)
                .ThenInclude(a => a!.ProfilePicture)
            .Include(g => g.Tenant)
            .OrderBy(g => g.FullName);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(List<Group> Items, int TotalCount)> GetMyGroupsPaged(Guid userId, int pageNumber, int pageSize)
    {
        var query = _dbContext.Groups
            .AsNoTracking()
            .AsSplitQuery()
            .Include(g => g.ApprovalStatus)
            .Include(g => g.Actor)
                .ThenInclude(a => a!.Pii)
            .Include(g => g.Actor)
                .ThenInclude(a => a!.ProfilePicture)
            .Include(g => g.Members)
                .ThenInclude(m => m.Role)
            .Where(g => g.Members.Any(m => m.UserId == userId))
            .OrderBy(g => g.FullName);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}

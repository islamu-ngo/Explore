// ABOUTME: Repository implementation for global user-role assignments.
// ABOUTME: Resolves instance-admin authority from platform-scoped admin roles.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class UserRoleRepository : GenericRepository<UserRole, Guid>, IUserRoleRepository
{
    private readonly ExploreDbContext _dbContext;

    public UserRoleRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> IsUserPlatformAdmin(Guid userId)
    {
        return await _dbContext.UserRoles
            .AsNoTracking()
            .Include(x => x.Role)
            .AnyAsync(x => x.UserId == userId
                && x.Role.Scope == RoleScopeEnum.Platform
                && x.Role.MasterCode == "platform.admin");
    }

    public async Task<UserRole?> GetByUserAndRole(Guid userId, int roleId)
    {
        return await _dbContext.UserRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.RoleId == roleId);
    }

    public async Task<bool> HasAnyPlatformAdmin()
    {
        return await _dbContext.UserRoles
            .AsNoTracking()
            .Include(x => x.Role)
            .AnyAsync(x => x.Role.Scope == RoleScopeEnum.Platform && x.Role.MasterCode == "platform.admin");
    }
}

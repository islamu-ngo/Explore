using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class OrganizationMemberRepository : GenericRepository<OrganizationMember, Guid>, IOrganizationMemberRepository
{
    private readonly ExploreDbContext _dbContext;

    public OrganizationMemberRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<OrganizationMember>> GetOrganizationMembersWithDetails()
    {
        return await _dbContext.OrganizationMembers
            .AsNoTracking()
            .AsSplitQuery()
            .Include(m => m.User)
            .Include(m => m.Organization)
            .Include(m => m.OrganizationRole)
            .Include(m => m.OrganizationPosition)
            .ToListAsync();
    }

    public async Task<OrganizationMember?> GetOrganizationMemberWithDetails(Guid id)
    {
        return await _dbContext.OrganizationMembers
            .AsNoTracking()
            .AsSplitQuery()
            .Include(m => m.User)
            .Include(m => m.Organization)
            .Include(m => m.OrganizationRole)
            .Include(m => m.OrganizationPosition)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<List<OrganizationMember>> GetMembersByOrganizationId(Guid organizationId)
    {
        return await _dbContext.OrganizationMembers
            .AsNoTracking()
            .AsSplitQuery()
            .Include(m => m.User)
            .Include(m => m.OrganizationRole)
            .Include(m => m.OrganizationPosition)
            .Where(m => m.OrganizationId == organizationId)
            .ToListAsync();
    }

    public async Task<List<User>> GetUsersByOrganization(Guid organizationId)
    {
        return await _dbContext.OrganizationMembers
            .AsNoTracking()
            .Include(m => m.User)
            .Where(m => m.OrganizationId == organizationId)
            .Select(m => m.User)
            .ToListAsync();
    }

    public async Task<List<Organization>> GetOrganizationsByUser(Guid userId)
    {
        return await _dbContext.OrganizationMembers
            .AsNoTracking()
            .Include(m => m.Organization)
            .Where(m => m.UserId == userId)
            .Select(m => m.Organization)
            .ToListAsync();
    }

    public async Task<List<OrganizationMember>> GetMembershipsByUser(Guid userId)
    {
        return await _dbContext.OrganizationMembers
            .AsNoTracking()
            .Include(m => m.Organization)
                .ThenInclude(o => o.ApprovalStatus)
            .Include(m => m.OrganizationRole)
            .Where(m => m.UserId == userId)
            .ToListAsync();
    }

    public async Task<bool> Exists(Guid organizationId, Guid userId)
    {
        return await _dbContext.OrganizationMembers
            .AsNoTracking()
            .AnyAsync(m => m.OrganizationId == organizationId && m.UserId == userId);
    }

    public async Task<List<OrganizationMember>> GetInvitesByEmail(string email)
    {
        return await _dbContext.OrganizationMembers
            .AsNoTracking()
            .Include(m => m.Organization)
            .Include(m => m.User)
            .Where(m => m.User.Email == email)
            .ToListAsync();
    }

    public async Task<OrganizationMember?> GetByOrganizationAndUser(Guid organizationId, Guid userId)
    {
        return await _dbContext.OrganizationMembers
            .AsNoTracking()
            .Include(m => m.OrganizationRole)
            .Include(m => m.Organization)
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == userId);
    }

    public async Task<bool> IsUserAdminOfOrganization(Guid organizationId, Guid userId)
    {
        // Admin-level roles: Creator, CoOwner, Admin
        var adminRoles = new[]
        {
            (int)OrganizationRoleEnum.Creator,
            (int)OrganizationRoleEnum.CoOwner,
            (int)OrganizationRoleEnum.Admin
        };

        return await _dbContext.OrganizationMembers
            .AsNoTracking()
            .AnyAsync(m => m.OrganizationId == organizationId
                && m.UserId == userId
                && adminRoles.Contains(m.OrganizationRoleId));
    }
}

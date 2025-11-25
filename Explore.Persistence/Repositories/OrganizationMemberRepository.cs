using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class OrganizationMemberRepository : GenericRepository<OrganizationMember, Guid>, IOrganizationMemberRepository
    {
        private readonly ExploreDbContext _dbContext;
        public OrganizationMemberRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<OrganizationMember>> GetOrganizationMembersWithDetails()
        {
            var organizationMembers = await _dbContext.OrganizationMembers
                .ToListAsync();
            return organizationMembers;
        }

        public async Task<OrganizationMember> GetOrganizationMemberWithDetails(Guid id)
        {
            var organizationMember = await _dbContext.OrganizationMembers
                .FirstOrDefaultAsync(o => o.Id == id);
            return organizationMember;
        }

        public async Task<List<OrganizationMember>> GetMembersByOrganizationId(Guid organizationId)
        {
            return await _dbContext.OrganizationMembers
                .Include(m => m.User)
                .Where(m => m.OrganizationId == organizationId)
                .ToListAsync();
        }

        public async Task<List<OrganizationMember>> GetInvitesByEmail(string email)
        {
            return await _dbContext.OrganizationMembers
                .Include(m => m.Organization)
                .Where(m => m.Email == email && m.UserId == null)
                .ToListAsync();
        }
    }
}

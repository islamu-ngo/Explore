using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IOrganizationMemberRepository : IGenericRepository<OrganizationMember, Guid>
    {
        Task<OrganizationMember> GetOrganizationMemberWithDetails(Guid id);
        Task<List<OrganizationMember>> GetOrganizationMembersWithDetails();
    }
}

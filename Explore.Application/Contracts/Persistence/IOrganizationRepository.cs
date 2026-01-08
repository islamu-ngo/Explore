using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IOrganizationRepository : IGenericRepository<Organization, Guid>
    {
        Task<Organization?> GetOrganizationWithDetails(Guid id);
        Task<List<Organization>> GetOrganizationsWithDetails();
        Task<List<Organization>> GetMyOrganizations(Guid userId);
    }
}

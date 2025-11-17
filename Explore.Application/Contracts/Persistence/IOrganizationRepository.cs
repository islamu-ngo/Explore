using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.DTOs.Organization;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IOrganizationRepository : IGenericRepository<Organization, Guid>
    {
        Task<OrganizationDto> GetOrganizationWithDetails(Guid id);
        Task<List<OrganizationListDto>> GetOrganizationsWithDetails();
    }
}

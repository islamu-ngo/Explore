using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Organization;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class OrganizationRepository : GenericRepository<Organization, Guid>, IOrganizationRepository
    {
        private readonly ExploreDbContext _dbContext;

        public OrganizationRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<OrganizationListDto>> GetOrganizationsWithDetails()
        {
            var organizations = await _dbContext.Organizations
                .Include(o => o.StatusType)
                .Select(o => new OrganizationListDto
                {
                    Id = o.Id,
                    FullName = o.FullName,
                    WebsiteUrl = o.WebsiteUrl,
                    Email = o.Email,
                    Country = o.Country,
                    City = o.City,
                    Postcode = o.Postcode,
                    Address = o.Address,
                    StatusTypeId = o.StatusTypeId,
                    StatusTypeFullName = o.StatusType.FullName
                })
                .ToListAsync();
            return organizations;
        }

        public async Task<OrganizationDto> GetOrganizationWithDetails(Guid id)
        {
            var organization = await _dbContext.Organizations
                .Include(o => o.StatusType)
                .Where(o => o.Id == id)
                .Select(o => new OrganizationDto
                {
                    Id = o.Id,
                    FullName = o.FullName,
                    WebsiteUrl = o.WebsiteUrl,
                    Email = o.Email,
                    Country = o.Country,
                    City = o.City,
                    Postcode = o.Postcode,
                    Address = o.Address,
                    StatusTypeId = o.StatusTypeId,
                    StatusTypeFullName = o.StatusType.FullName
                })
                .FirstOrDefaultAsync();
            return organization;
        }

        public async Task<List<OrganizationListDto>> GetAllWithStatusAsync()
        {
            // Haal alle organisaties op met status info voor admin dashboard
            var organizations = await _dbContext.Organizations
                .Include(o => o.StatusType)
                .OrderBy(o => o.Id) // orderd by id later with created at
                .Select(o => new OrganizationListDto
                {
                    Id = o.Id,
                    FullName = o.FullName,
                    WebsiteUrl = o.WebsiteUrl,
                    Email = o.Email,
                    Country = o.Country,
                    City = o.City,
                    Postcode = o.Postcode,
                    Address = o.Address,
                    StatusTypeId = o.StatusTypeId,
                    StatusTypeFullName = o.StatusType.FullName
                })
                .ToListAsync();
            return organizations;
        }
    }
}

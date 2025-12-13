using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Organization;
using Explore.Domain;
using Explore.Domain.Enums;
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
                    StatusTypeFullName = o.StatusType.FullName,
                    CreatedAt = o.CreatedAt
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
                    StatusTypeFullName = o.StatusType.FullName,
                    CreatedByUserId = o.CreatedByUserId,
                    CreatedAt = o.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (organization != null && !string.IsNullOrEmpty(organization.CreatedByUserId) && Guid.TryParse(organization.CreatedByUserId, out Guid userId))
            {
                var user = await _dbContext.Users.FindAsync(userId);
                if (user != null)
                {
                    organization.CreatorUserName = user.Username;
                    organization.CreatorEmail = user.Email;
                }
            }

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
                    StatusTypeFullName = o.StatusType.FullName,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();
            return organizations;
        }

        public async Task<List<OrganizationListDto>> GetMyOrganizations(string userId)
        {
            Guid userGuid;
            bool isGuid = Guid.TryParse(userId, out userGuid);

            // Haal organisaties op die door deze gebruiker zijn aangemaakt OF waar de gebruiker lid van is
            var query = _dbContext.Organizations
                .Include(o => o.StatusType)
                .Include(o => o.Members)
                .AsQueryable();

            if (isGuid)
            {
                query = query.Where(o => o.CreatedByUserId == userId || o.Members.Any(m => m.UserId == userGuid));
            }
            else
            {
                query = query.Where(o => o.CreatedByUserId == userId);
            }

            var organizations = await query
                .OrderByDescending(o => o.CreatedAt)
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
                    StatusTypeFullName = o.StatusType.FullName,
                    CreatedAt = o.CreatedAt,
                    CurrentUserRole = o.CreatedByUserId == userId ? OrganizationRole.Creator : 
                                      (isGuid ? o.Members.Where(m => m.UserId == userGuid).Select(m => (OrganizationRole?)m.Role).FirstOrDefault() : null)
                })
                .ToListAsync();
            return organizations;
        }
    }
}

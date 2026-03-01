// ABOUTME: Repository interface for OrganizationSetting entity providing data access
// for organization-specific setting overrides.

namespace Explore.Application.Contracts.Persistence;

using Explore.Domain;

/// <summary>
/// Repository for organization-specific setting overrides.
/// </summary>
public interface IOrganizationSettingRepository : IGenericRepository<OrganizationSetting, Guid>
{
    Task<OrganizationSetting?> GetByOrganizationAndKey(Guid organizationId, string key);
    Task<List<OrganizationSetting>> GetAllForOrganization(Guid organizationId);
    Task<bool> RemoveOverride(Guid organizationId, string key);
}

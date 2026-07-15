// ABOUTME: DTO returned by GET /api/User/admin-authority to convey persisted administrative authority.
// ABOUTME: Covers instance, tenant, organization, and group scopes for BFF and route authorization.

namespace Explore.Application.DTOs.User;

/// <summary>
/// Represents the admin authority of a user across the instance, tenant, organization, and group hierarchy.
/// Returned by the admin-authority API endpoint and consumed by the BFF's claims transformation.
/// </summary>
public class AdminAuthorityDto
{
    /// <summary>Whether the user is an Instance Administrator (platform-scoped).</summary>
    public bool IsInstanceAdmin { get; set; }

    /// <summary>Tenant IDs where the user has tenant-level admin rights.</summary>
    public List<Guid> AdminTenantIds { get; set; } = [];

    /// <summary>Organization IDs where the user has organization-level admin rights (Creator, CoOwner, Admin).</summary>
    public List<Guid> AdminOrganizationIds { get; set; } = [];

    /// <summary>Group IDs where the user has group-level admin rights (Creator or Admin).</summary>
    public List<Guid> AdminGroupIds { get; set; } = [];

    /// <summary>True if the user has any admin authority at any level.</summary>
    public bool HasAnyAuthority => IsInstanceAdmin
        || AdminTenantIds.Count > 0
        || AdminOrganizationIds.Count > 0
        || AdminGroupIds.Count > 0;
}

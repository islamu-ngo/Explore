// ABOUTME: Builds a CerbosPrincipal DTO from the current user's IAdminContext authority profile.
// ABOUTME: Extracted from CerbosAuthorizationService for independent unit testing.

using Explore.Application.Contracts.Identity;

namespace Explore.Infrastructure.Services;

/// <summary>
/// Constructs the principal DTO sent to the Cerbos PDP for authorization checks.
/// Resolves the user's instance-admin status, tenant memberships, and organization memberships
/// from <see cref="IAdminContext"/> and maps them into the Cerbos attribute structure.
/// </summary>
public class CerbosPrincipalBuilder
{
    private readonly IAdminContext _adminContext;

    public CerbosPrincipalBuilder(IAdminContext adminContext)
    {
        _adminContext = adminContext;
    }

    /// <summary>
    /// Builds a <see cref="CerbosPrincipal"/> for the given user by querying their
    /// administrative authority across instance, tenant, and organization scopes.
    /// </summary>
    public async Task<CerbosPrincipal> BuildAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var isInstanceAdmin = await _adminContext.IsInstanceAdminAsync(cancellationToken);
        var adminTenantIds = await _adminContext.GetAdminTenantIdsAsync(cancellationToken);
        var adminOrgIds = await _adminContext.GetAdminOrganizationIdsAsync(cancellationToken);

        var tenantMemberships = adminTenantIds
            .ToDictionary(id => id.ToString(), _ => (object)"admin");
        var orgMemberships = adminOrgIds
            .ToDictionary(id => id.ToString(), _ => (object)"admin");

        return new CerbosPrincipal
        {
            Id = userId.ToString(),
            Roles = ["authenticated_user"],
            Attr = new Dictionary<string, object>
            {
                ["isInstanceAdmin"] = isInstanceAdmin,
                ["tenantMemberships"] = tenantMemberships,
                ["orgMemberships"] = orgMemberships
            }
        };
    }
}

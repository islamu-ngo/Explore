// ABOUTME: Builds a Cerbos SDK Principal from the current user's IAdminContext authority profile.
// ABOUTME: Extracted from CerbosAuthorizationService for independent unit testing.

using Cerbos.Sdk.Builder;
using Explore.Application.Contracts.Identity;

namespace Explore.Infrastructure.Services;

/// <summary>
/// Constructs the <see cref="Principal"/> sent to the Cerbos PDP for authorization checks.
/// Resolves the user's instance-admin status, tenant memberships, and organization memberships
/// from <see cref="IAdminContext"/> and maps them into the Cerbos SDK attribute structure.
/// </summary>
public class CerbosPrincipalBuilder
{
    private readonly IAdminContext _adminContext;

    public CerbosPrincipalBuilder(IAdminContext adminContext)
    {
        _adminContext = adminContext;
    }

    /// <summary>
    /// Builds a Cerbos SDK <see cref="Principal"/> for the given user by querying their
    /// administrative authority across instance, tenant, and organization scopes.
    /// </summary>
    public async Task<Principal> BuildSdkPrincipalAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var isInstanceAdmin = await _adminContext.IsInstanceAdminAsync(cancellationToken);
        var adminTenantIds = await _adminContext.GetAdminTenantIdsAsync(cancellationToken);
        var adminOrgIds = await _adminContext.GetAdminOrganizationIdsAsync(cancellationToken);

        var tenantMemberships = adminTenantIds
            .ToDictionary(id => id.ToString(), _ => AttributeValue.StringValue("admin"));
        var orgMemberships = adminOrgIds
            .ToDictionary(id => id.ToString(), _ => AttributeValue.StringValue("admin"));

        return Principal
            .NewInstance(userId.ToString(), "authenticated_user")
            .WithAttribute("isInstanceAdmin", AttributeValue.BoolValue(isInstanceAdmin))
            .WithAttribute("tenantMemberships", AttributeValue.MapValue(tenantMemberships))
            .WithAttribute("orgMemberships", AttributeValue.MapValue(orgMemberships));
    }
}

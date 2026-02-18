// ABOUTME: Route guard that restricts org admin routes to users with org-admin authority for the specific org.
// Also allows instance and tenant admins who have broader authority over all organizations.

using System.Text.RegularExpressions;
using Blazouter.Interfaces;
using Blazouter.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace Explore.Blazor.Client.Routing.Guards;

/// <summary>
/// Guards organization admin routes by verifying the user has admin authority for the
/// specific organization in the route. Checks for:
/// 1. Instance admin (full access to all orgs)
/// 2. Tenant admin (full access to orgs in their tenant)
/// 3. Organization admin (access to their specific org)
/// The organization ID is extracted from the route path via regex.
/// </summary>
public sealed partial class OrgAdminRouteGuard(AuthenticationStateProvider authStateProvider) : IRouteGuard
{
    // Claim type constants matching Explore.Application.Authorization.AdminClaimTypes.
    // Duplicated here because Blazor.Client does not reference Application.
    private const string InstanceAdminClaim = "explore:admin:instance";
    private const string TenantAdminClaim = "explore:admin:tenant";
    private const string OrgAdminClaim = "explore:admin:organization";

    public async Task<bool> CanActivateAsync(RouteMatch match)
    {
        if (authStateProvider is null)
        {
            return false;
        }

        var authState = await authStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false);
        var user = authState.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        // Instance admins and tenant admins have broader authority
        if (user.HasClaim(c => c.Type == InstanceAdminClaim)
            || user.HasClaim(c => c.Type == TenantAdminClaim))
        {
            return true;
        }

        // Extract organization ID from the route path (e.g., /admin/organization/{guid}/settings)
        var orgId = ExtractOrgIdFromPath(match.MatchedPath);
        if (orgId is null)
        {
            return false;
        }

        return user.HasClaim(c => c.Type == OrgAdminClaim
                                  && string.Equals(c.Value, orgId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<string?> GetRedirectPathAsync(RouteMatch match)
    {
        if (authStateProvider is not null)
        {
            var authState = await authStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false);
            if (authState?.User?.Identity?.IsAuthenticated == true)
            {
                return "/";
            }
        }

        var returnUrl = string.IsNullOrWhiteSpace(match.MatchedPath)
            ? "/"
            : match.MatchedPath;

        return $"/login?returnUrl={Uri.EscapeDataString(returnUrl)}";
    }

    private static string? ExtractOrgIdFromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        // Match GUID segment after /organization/ in the path
        var match = OrgIdPattern().Match(path);
        return match.Success ? match.Groups[1].Value : null;
    }

    [GeneratedRegex(@"/organization/([0-9a-fA-F\-]{36})", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex OrgIdPattern();
}

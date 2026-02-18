// ABOUTME: Enriches authenticated ClaimsPrincipal with DB-resolved admin authority claims.
// Bridges the gap between server-side IAdminContext and Blazor WASM via claim serialization.

using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Identity;

/// <summary>
/// Transforms the authenticated ClaimsPrincipal by adding admin authority claims
/// resolved from the database via <see cref="IAdminContext"/>.
/// These claims are serialized to Blazor WASM via <c>AddAuthenticationStateSerialization</c>,
/// enabling the frontend to check admin authority without additional API calls.
/// </summary>
public sealed class AdminClaimsTransformation : IClaimsTransformation
{
    private readonly IAdminContext _adminContext;
    private readonly ILogger<AdminClaimsTransformation> _logger;

    public AdminClaimsTransformation(
        IAdminContext adminContext,
        ILogger<AdminClaimsTransformation> logger)
    {
        _adminContext = adminContext;
        _logger = logger;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return principal;
        }

        // Skip if admin claims are already present (avoid duplicate transformation)
        if (principal.HasClaim(c => c.Type == AdminClaimTypes.InstanceAdmin
                                    || c.Type == AdminClaimTypes.TenantAdmin
                                    || c.Type == AdminClaimTypes.OrganizationAdmin))
        {
            return principal;
        }

        // Extract userId from the principal parameter, NOT from HttpContext.User.
        // During IClaimsTransformation, HttpContext.User hasn't been set to the
        // authenticated principal yet, so IAdminContext.UserId returns null.
        var sub = principal.FindFirst("sub")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sid")?.Value;

        if (!Guid.TryParse(sub, out var userId))
        {
            return principal;
        }

        var identity = new ClaimsIdentity();

        try
        {
            // Resolve instance admin authority
            var isInstanceAdmin = await _adminContext.IsInstanceAdminAsync(userId);
            if (isInstanceAdmin)
            {
                identity.AddClaim(new Claim(AdminClaimTypes.InstanceAdmin, "true"));
            }

            // Resolve tenant admin authority (one claim per tenant)
            var tenantIds = await _adminContext.GetAdminTenantIdsAsync(userId);
            foreach (var tenantId in tenantIds)
            {
                identity.AddClaim(new Claim(AdminClaimTypes.TenantAdmin, tenantId.ToString()));
            }

            // Resolve organization admin authority (one claim per organization)
            var orgIds = await _adminContext.GetAdminOrganizationIdsAsync(userId);
            foreach (var orgId in orgIds)
            {
                identity.AddClaim(new Claim(AdminClaimTypes.OrganizationAdmin, orgId.ToString()));
            }

            if (identity.Claims.Any())
            {
                principal.AddIdentity(identity);
                _logger.LogDebug(
                    "AdminClaimsTransformation: Added {ClaimCount} admin claims for user {UserId} " +
                    "(instance={IsInstance}, tenants={TenantCount}, orgs={OrgCount})",
                    identity.Claims.Count(), userId, isInstanceAdmin, tenantIds.Count, orgIds.Count);
            }
        }
        catch (Exception ex)
        {
            // Fail open for claims transformation — log the error but don't block authentication.
            // The authorization layer (Cerbos/MediatR behavior) provides the hard security boundary.
            _logger.LogWarning(ex,
                "AdminClaimsTransformation: Failed to resolve admin authority for user {UserId}. " +
                "Admin UI will be hidden but server-side authorization remains enforced.", userId);
        }

        return principal;
    }
}

// ABOUTME: Enriches authenticated ClaimsPrincipal with DB-resolved admin authority claims.
// Keeps DB-backed admin authority available to server-side BFF/API authorization decisions.

using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Domain.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Identity;

/// <summary>
/// Transforms the authenticated ClaimsPrincipal by adding admin authority claims
/// resolved from the database via <see cref="IAdminContext"/>.
/// These claims remain server-side; browser UI authority must come from BFF/API/HAL/status endpoints.
/// </summary>
public sealed class AdminClaimsTransformation : IClaimsTransformation
{
    private const string InternalUserIdClaimType = "internal_user_id";

    private readonly IAdminContext _adminContext;
    private readonly IUserExternalLoginRepository _userExternalLoginRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AdminClaimsTransformation> _logger;

    public AdminClaimsTransformation(
        IAdminContext adminContext,
        IUserExternalLoginRepository userExternalLoginRepository,
        IUserRepository userRepository,
        ILogger<AdminClaimsTransformation> logger)
    {
        _adminContext = adminContext;
        _userExternalLoginRepository = userExternalLoginRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return principal;
        }

        var userId = await ResolveInternalUserIdAsync(principal);
        if (!userId.HasValue)
        {
            return principal;
        }

        if (!principal.HasClaim(c => c.Type == InternalUserIdClaimType))
        {
            var internalIdentity = new ClaimsIdentity();
            internalIdentity.AddClaim(new Claim(InternalUserIdClaimType, userId.Value.ToString()));
            principal.AddIdentity(internalIdentity);
        }

        if (principal.HasClaim(c => c.Type == AdminClaimTypes.InstanceAdmin
                                    || c.Type == AdminClaimTypes.TenantAdmin
                                    || c.Type == AdminClaimTypes.OrganizationAdmin))
        {
            return principal;
        }

        var identity = new ClaimsIdentity();

        try
        {
            // Resolve instance admin authority
            var isInstanceAdmin = await _adminContext.IsInstanceAdminAsync(userId.Value);
            if (isInstanceAdmin)
            {
                identity.AddClaim(new Claim(AdminClaimTypes.InstanceAdmin, "true"));
            }

            // Resolve tenant admin authority (one claim per tenant)
            var tenantIds = await _adminContext.GetAdminTenantIdsAsync(userId.Value);
            foreach (var tenantId in tenantIds)
            {
                identity.AddClaim(new Claim(AdminClaimTypes.TenantAdmin, tenantId.ToString()));
            }

            // Resolve organization admin authority (one claim per organization)
            var orgIds = await _adminContext.GetAdminOrganizationIdsAsync(userId.Value);
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
                    identity.Claims.Count(), userId.Value, isInstanceAdmin, tenantIds.Count, orgIds.Count);
            }
        }
        catch (Exception ex)
        {
            // Fail open for claims transformation — log the error but don't block authentication.
            // The authorization layer (Cerbos/MediatR behavior) provides the hard security boundary.
            _logger.LogWarning(ex,
                "AdminClaimsTransformation: Failed to resolve admin authority for user {UserId}. " +
                "Admin UI will be hidden but server-side authorization remains enforced.", userId.Value);
        }

        return principal;
    }

    private async Task<Guid?> ResolveInternalUserIdAsync(ClaimsPrincipal principal)
    {
        var internalUserIdClaim = principal.FindFirst(InternalUserIdClaimType)?.Value;
        if (Guid.TryParse(internalUserIdClaim, out var internalUserId))
        {
            return internalUserId;
        }

        var subject = principal.FindFirst("sub")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sid")?.Value;

        if (string.IsNullOrWhiteSpace(subject))
        {
            return null;
        }

        if (Guid.TryParse(subject, out var subjectAsGuid))
        {
            return subjectAsGuid;
        }

        var provider = ResolveAuthProvider(principal, subject);
        if (string.IsNullOrWhiteSpace(provider))
        {
            return null;
        }

        var providerId = ResolveProviderId(principal, subject, provider);
        if (string.IsNullOrWhiteSpace(providerId))
        {
            return null;
        }

        var externalLogin = await _userExternalLoginRepository.GetByProviderAndKey(provider, providerId);
        if (externalLogin != null)
        {
            return externalLogin.UserId;
        }

        if (SupportsEmailAutoMatch(provider) && ResolveEmailVerified(principal))
        {
            var email = principal.FindFirst("email")?.Value
                ?? principal.FindFirst(ClaimTypes.Email)?.Value;

            if (!string.IsNullOrWhiteSpace(email))
            {
                var user = await _userRepository.GetUserByEmail(email.Trim().ToLowerInvariant());
                if (user != null)
                {
                    return user.Id;
                }
            }
        }

        return null;
    }

    private static string ResolveAuthProvider(ClaimsPrincipal principal, string subject)
    {
        var idp = principal.FindFirst("idp")?.Value;
        if (!string.IsNullOrWhiteSpace(idp))
        {
            var normalizedIdp = idp.Trim().ToLowerInvariant();
            if (normalizedIdp.Contains("google"))
            {
                return AuthSchemeNames.Google.ToLowerInvariant();
            }

            if (normalizedIdp.Contains("atproto"))
            {
                return AuthSchemeNames.Atproto.ToLowerInvariant();
            }

            if (normalizedIdp.Contains("keycloak"))
            {
                return AuthSchemeNames.Keycloak.ToLowerInvariant();
            }
        }

        var issuer = principal.FindFirst("iss")?.Value;
        if (!string.IsNullOrWhiteSpace(issuer))
        {
            var normalizedIssuer = issuer.Trim().ToLowerInvariant();
            if (normalizedIssuer.Contains("accounts.google.com"))
            {
                return AuthSchemeNames.Google.ToLowerInvariant();
            }

            if (normalizedIssuer.Contains("keycloak") || normalizedIssuer.Contains("/realms/"))
            {
                return AuthSchemeNames.Keycloak.ToLowerInvariant();
            }
        }

        if (subject.StartsWith("did:", StringComparison.OrdinalIgnoreCase))
        {
            return AuthSchemeNames.Atproto.ToLowerInvariant();
        }

        return string.Empty;
    }

    private static string ResolveProviderId(ClaimsPrincipal principal, string subject, string provider)
    {
        if (provider == AuthSchemeNames.Atproto.ToLowerInvariant())
        {
            return principal.FindFirst("did")?.Value
                ?? principal.FindFirst("atproto_did")?.Value
                ?? subject;
        }

        return subject;
    }

    private static bool ResolveEmailVerified(ClaimsPrincipal principal)
    {
        var raw = principal.FindFirst("email_verified")?.Value;
        return bool.TryParse(raw, out var emailVerified)
            ? emailVerified
            : string.Equals(raw, "1", StringComparison.OrdinalIgnoreCase);
    }

    private static bool SupportsEmailAutoMatch(string provider)
    {
        return provider == AuthSchemeNames.Keycloak.ToLowerInvariant()
            || provider == AuthSchemeNames.Google.ToLowerInvariant();
    }
}

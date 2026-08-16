// ABOUTME: Canonical ClaimsPrincipal reading for platform user identity and provider bootstrap identity.
// ABOUTME: Single authority for the documented user-id fallback chain and provider account reconstruction.

using System.Security.Claims;
using Explore.Domain.Constants;

namespace Explore.Application.Authentication;

/// <summary>
/// The one place that turns an authenticated <see cref="ClaimsPrincipal"/> into platform identity.
/// <para>
/// Identity derivation is a pure function of the principal, so callers that already hold one — controllers
/// through <c>ControllerBase.User</c>, middleware through <c>HttpContext.User</c>, infrastructure through
/// <c>IHttpContextAccessor</c> — never need to resolve a service to ask who the caller is.
/// </para>
/// <para>
/// Purpose-bound schemes (API key, setup secret, managed control plane, ATProto session, privacy-erasure
/// receipt) deliberately do not route through here. Their claims are protocol validation at the
/// authentication boundary, not ambient user identity, and collapsing them together would widen trust.
/// </para>
/// </summary>
public static class PlatformIdentityPrincipalExtensions
{
    private const string InternalUserIdClaimType = "internal_user_id";

    /// <summary>
    /// Resolves the platform user id using the documented fallback chain
    /// <c>sub → nameidentifier → sid → internal_user_id</c>, accepting only GUID-parseable values.
    /// <para>
    /// The provider claims are tried before <c>internal_user_id</c> on purpose: for platform-managed accounts
    /// the provider subject <em>is</em> the local id, and preferring it keeps one identifier authoritative.
    /// A principal whose provider subject is not a GUID — an ATProto DID or a Google subject — falls through
    /// to <c>internal_user_id</c>, and if that is absent the caller must resolve the local account through
    /// <see cref="CurrentUserResolutionExtensions.ResolveCurrentUserIdAsync"/> instead of guessing.
    /// </para>
    /// </summary>
    public static Guid? GetPlatformUserId(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        string?[] candidates =
        [
            principal.FindFirst("sub")?.Value,
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            principal.FindFirst("sid")?.Value,
            principal.FindFirst(InternalUserIdClaimType)?.Value,
        ];

        foreach (var candidate in candidates)
        {
            if (Guid.TryParse(candidate, out var userId))
            {
                return userId;
            }
        }

        return null;
    }

    /// <exception cref="UnauthorizedAccessException">The principal carries no usable platform user id.</exception>
    public static Guid GetRequiredPlatformUserId(this ClaimsPrincipal principal) =>
        principal.GetPlatformUserId()
        ?? throw new UnauthorizedAccessException("User is not authenticated or user ID is not available in the token.");

    public static string? GetEmail(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        return principal.FindFirst("email")?.Value ?? principal.FindFirst(ClaimTypes.Email)?.Value;
    }

    public static string? GetUsername(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        return principal.FindFirst("preferred_username")?.Value ?? principal.FindFirst(ClaimTypes.Name)?.Value;
    }

    public static string? GetFirstName(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        return principal.FindFirst("given_name")?.Value ?? principal.FindFirst(ClaimTypes.GivenName)?.Value;
    }

    public static string? GetLastName(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        return principal.FindFirst("family_name")?.Value ?? principal.FindFirst(ClaimTypes.Surname)?.Value;
    }

    /// <summary>
    /// Reconstructs the external provider account behind this principal for first-login bootstrap and account
    /// sync. Returns <see langword="null"/> when no provider subject is present, which callers must treat as
    /// unauthenticated rather than falling back to a partially populated identity.
    /// </summary>
    public static ProviderIdentity? GetProviderIdentity(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var subject = principal.GetProviderSubject();
        if (string.IsNullOrWhiteSpace(subject))
        {
            return null;
        }

        var provider = principal.GetAuthProvider();
        var email = principal.GetEmail() ?? string.Empty;

        return new ProviderIdentity(
            subject,
            provider,
            principal.GetProviderId(subject, provider),
            email,
            principal.GetEmailVerified(provider, email));
    }

    /// <summary>The provider's stable subject claim, using the same chain as the platform user id.</summary>
    public static string? GetProviderSubject(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return principal.FindFirst("sub")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sid")?.Value;
    }

    /// <summary>
    /// Classifies the issuing provider from the explicit <c>idp</c> claim, then the issuer, then the subject
    /// shape. Keycloak is the default because it is the platform-managed identity provider.
    /// </summary>
    public static string GetAuthProvider(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var explicitProvider = principal.FindFirst("idp")?.Value;
        if (!string.IsNullOrWhiteSpace(explicitProvider))
        {
            var normalized = explicitProvider.Trim().ToLowerInvariant();
            if (normalized.Contains("google", StringComparison.Ordinal))
            {
                return AuthSchemeNames.Google.ToLowerInvariant();
            }

            if (normalized.Contains("atproto", StringComparison.Ordinal))
            {
                return AuthSchemeNames.Atproto.ToLowerInvariant();
            }

            if (normalized.Contains("keycloak", StringComparison.Ordinal))
            {
                return AuthSchemeNames.Keycloak.ToLowerInvariant();
            }
        }

        var issuer = principal.FindFirst("iss")?.Value ?? string.Empty;
        if (issuer.Contains("accounts.google.com", StringComparison.OrdinalIgnoreCase))
        {
            return AuthSchemeNames.Google.ToLowerInvariant();
        }

        var subject = principal.GetProviderSubject() ?? string.Empty;
        if (subject.StartsWith("did:", StringComparison.OrdinalIgnoreCase) ||
            issuer.Contains("atproto", StringComparison.OrdinalIgnoreCase))
        {
            return AuthSchemeNames.Atproto.ToLowerInvariant();
        }

        return AuthSchemeNames.Keycloak.ToLowerInvariant();
    }

    /// <summary>ATProto identities are keyed by DID rather than by the raw subject claim.</summary>
    public static string GetProviderId(this ClaimsPrincipal principal, string providerSubject, string provider)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (string.Equals(provider, AuthSchemeNames.Atproto.ToLowerInvariant(), StringComparison.Ordinal))
        {
            return principal.FindFirst("did")?.Value
                ?? principal.FindFirst("atproto_did")?.Value
                ?? providerSubject;
        }

        return providerSubject;
    }

    /// <summary>
    /// Honors an explicit <c>email_verified</c> claim, otherwise defaults per provider: the OIDC providers
    /// verify addresses themselves, while ATProto carries no email guarantee and must stay unverified.
    /// </summary>
    public static bool GetEmailVerified(this ClaimsPrincipal principal, string provider, string email)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (bool.TryParse(principal.FindFirst("email_verified")?.Value, out var emailVerified))
        {
            return emailVerified;
        }

        return provider switch
        {
            "keycloak" => true,
            "google" => true,
            "atproto" => false,
            _ => !string.IsNullOrWhiteSpace(email),
        };
    }
}

/// <summary>External provider account behind an authenticated principal, used for bootstrap and sync only.</summary>
public sealed record ProviderIdentity(
    string Subject,
    string Provider,
    string ProviderId,
    string Email,
    bool EmailVerified);

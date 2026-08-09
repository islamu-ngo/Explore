// ABOUTME: Abstract base controller providing centralized user identity access via IUserContext.
// ABOUTME: Eliminates duplicated GetCurrentUserId methods across controllers.

using System.Security.Claims;
using Explore.Application.Contracts.Identity;
using Explore.Application.Features.Users.Requests.Queries;
using Explore.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

public abstract class ExploreControllerBase : ControllerBase
{
    private IUserContext? _userContext;

    protected IUserContext UserContext => _userContext ??= HttpContext.RequestServices.GetRequiredService<IUserContext>();

    protected Guid? CurrentUserId => UserContext.UserId;

    /// <exception cref="UnauthorizedAccessException">User is not authenticated.</exception>
    protected Guid RequiredUserId => UserContext.GetRequiredUserId();

    protected static bool TryParseConcurrencyStamp(string? ifMatch, out Guid concurrencyStamp)
    {
        concurrencyStamp = default;
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return false;
        }

        var value = ifMatch.Trim();
        if (value.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        value = value.Trim('"');
        return Guid.TryParse(value, out concurrencyStamp) && concurrencyStamp != Guid.Empty;
    }

    protected string? ResolveProviderSubject()
    {
        return User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sid")?.Value;
    }

    protected string ResolveAuthProvider()
    {
        var explicitProvider = User.FindFirst("idp")?.Value;
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

        var issuer = User.FindFirst("iss")?.Value ?? string.Empty;
        if (issuer.Contains("accounts.google.com", StringComparison.OrdinalIgnoreCase))
        {
            return AuthSchemeNames.Google.ToLowerInvariant();
        }

        var subject = ResolveProviderSubject() ?? string.Empty;
        if (subject.StartsWith("did:", StringComparison.OrdinalIgnoreCase) ||
            issuer.Contains("atproto", StringComparison.OrdinalIgnoreCase))
        {
            return AuthSchemeNames.Atproto.ToLowerInvariant();
        }

        return AuthSchemeNames.Keycloak.ToLowerInvariant();
    }

    protected string ResolveProviderId(string providerSubject, string provider)
    {
        if (provider == AuthSchemeNames.Atproto.ToLowerInvariant())
        {
            return User.FindFirst("did")?.Value
                ?? User.FindFirst("atproto_did")?.Value
                ?? providerSubject;
        }

        return providerSubject;
    }

    protected bool ResolveEmailVerified(string provider, string email)
    {
        var emailVerifiedClaim = User.FindFirst("email_verified")?.Value;
        if (bool.TryParse(emailVerifiedClaim, out var emailVerified))
        {
            return emailVerified;
        }

        return provider switch
        {
            "keycloak" => true,
            "google" => true,
            "atproto" => false,
            _ => !string.IsNullOrWhiteSpace(email)
        };
    }

    protected async Task<Guid?> ResolveCurrentUserIdAsync(IMediator mediator, CancellationToken cancellationToken = default)
    {
        var internalUserIdClaim = User.FindFirst("internal_user_id")?.Value;
        if (Guid.TryParse(internalUserIdClaim, out var internalUserId))
        {
            return internalUserId;
        }

        var providerSubject = ResolveProviderSubject();
        if (string.IsNullOrWhiteSpace(providerSubject))
        {
            return null;
        }

        var provider = ResolveAuthProvider();
        var providerId = ResolveProviderId(providerSubject, provider);
        var email = User.FindFirst("email")?.Value
            ?? User.FindFirst(ClaimTypes.Email)?.Value
            ?? string.Empty;
        var emailVerified = ResolveEmailVerified(provider, email);

        var resolveQuery = new ResolveCurrentUserIdByIdentityRequest
        {
            Provider = provider,
            ProviderId = providerId,
            Email = email,
            EmailVerified = emailVerified
        };

        return await mediator.Send(resolveQuery, cancellationToken);
    }
}

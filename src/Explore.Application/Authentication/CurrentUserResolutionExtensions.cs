// ABOUTME: Single authority for resolving the local user id behind a provider-authenticated principal.
// ABOUTME: Composes the canonical claim chain with the provider-link query so callers never parse claims.

using System.Security.Claims;
using Explore.Application.Features.Users.Requests.Queries;
using MediatR;

namespace Explore.Application.Authentication;

/// <summary>
/// Resolution for principals whose provider subject is not itself a platform user id — ATProto DIDs and
/// Google subjects, chiefly. It is an extension over <see cref="IMediator"/> rather than an injected service
/// because both inputs, the mediator and the principal, are already in hand at every call site; adding a
/// constructor dependency would buy nothing and adding a base-class helper would hide the query behind
/// inheritance.
/// </summary>
public static class CurrentUserResolutionExtensions
{
    /// <summary>
    /// Returns the local user id for the caller, or <see langword="null"/> when the principal carries no
    /// provider identity or no local account is linked yet. A null result is an authentication outcome for
    /// the caller to map — never a reason to fall back to a different identity source.
    /// </summary>
    public static async Task<Guid?> ResolveCurrentUserIdAsync(
        this IMediator mediator,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mediator);
        ArgumentNullException.ThrowIfNull(principal);

        if (principal.GetAmbientPlatformIdentity() is null)
        {
            return null;
        }

        // A principal that already carries the local id needs no lookup; this is the common authenticated path.
        if (principal.GetPlatformUserId() is { } platformUserId)
        {
            return platformUserId;
        }

        var providerIdentity = principal.GetProviderIdentity();
        if (providerIdentity is null)
        {
            return null;
        }

        return await mediator.Send(
            new ResolveCurrentUserIdByIdentityRequest
            {
                Provider = providerIdentity.Provider,
                ProviderId = providerIdentity.ProviderId,
                Email = providerIdentity.Email,
                EmailVerified = providerIdentity.EmailVerified,
            },
            cancellationToken);
    }
}

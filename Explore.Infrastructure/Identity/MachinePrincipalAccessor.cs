// ABOUTME: Reads the current HTTP request's ClaimsPrincipal and exposes any API-key-derived machine principal context.
// ABOUTME: Delegates claim parsing to ApiAuthenticationPrincipalExtensions for a single parsing contract.

using Explore.Application.Authentication;
using Explore.Application.Contracts.Identity;
using Microsoft.AspNetCore.Http;

namespace Explore.Infrastructure.Identity;

/// <summary>
/// Resolves the machine principal context from the current <see cref="HttpContext.User"/> when the request
/// was authenticated via an external API key. Returns <c>null</c> for JWT, anonymous, or absent contexts.
/// </summary>
public sealed class MachinePrincipalAccessor : IMachinePrincipalAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MachinePrincipalAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ApiKeyPrincipalContext? Current
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            return user.TryGetApiKeyPrincipalContext();
        }
    }

    public bool IsMachineCaller => Current is not null;
}

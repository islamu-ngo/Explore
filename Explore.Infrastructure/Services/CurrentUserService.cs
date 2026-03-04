using System.Security.Claims;
using Explore.Application.Contracts.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace Explore.Infrastructure.Services;

/// <summary>
/// Provides access to the current authenticated user's information from HTTP context.
/// Implements Clean Architecture by abstracting HTTP concerns from the Application layer.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private const string InternalUserIdClaimType = "internal_user_id";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Gets the current user's ID from authentication claims.
    /// Tries multiple claim types for compatibility with different auth providers.
    /// Priority: sub → nameidentifier → sid
    /// </summary>
    public Guid? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity?.IsAuthenticated == true)
            {
                return null;
            }

            // Try standard OIDC "sub" claim first, then fallback to other claim types
            var userIdClaim = user.FindFirst(InternalUserIdClaimType)?.Value
                ?? user.FindFirst("sub")?.Value
                ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("sid")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return null;
            }

            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    /// <summary>
    /// Gets whether a user is currently authenticated.
    /// </summary>
    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
}

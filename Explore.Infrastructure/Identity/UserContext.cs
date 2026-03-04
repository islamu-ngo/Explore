using System.Security.Claims;
using Explore.Application.Contracts.Identity;
using Microsoft.AspNetCore.Http;

namespace Explore.Infrastructure.Identity;

/// <summary>
/// Implementation of IUserContext that extracts user information from HTTP context.
/// Works with Keycloak JWT tokens.
/// </summary>
public class UserContext : IUserContext
{
    private const string InternalUserIdClaimType = "internal_user_id";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var userIdClaim = User?.FindFirst(InternalUserIdClaimType)?.Value
                ?? User?.FindFirst("sub")?.Value
                ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User?.FindFirst("sid")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return null;

            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public string? Email => User?.FindFirst("email")?.Value
        ?? User?.FindFirst(ClaimTypes.Email)?.Value;

    public string? Username => User?.FindFirst("preferred_username")?.Value
        ?? User?.FindFirst(ClaimTypes.Name)?.Value;

    public Guid GetRequiredUserId()
    {
        var userId = UserId;
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated or user ID is not available in the token.");
        }
        return userId.Value;
    }
}

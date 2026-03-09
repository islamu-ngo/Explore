// ABOUTME: Internal Phase 0 probe endpoint for validating authentication plus tenant-resolution flow.
// ABOUTME: Returns the resolved runtime context for integration tests without exposing the endpoint in API docs.

using System.Security.Claims;
using Explore.Application.Constants;
using Explore.Application.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/_internal/auth-probe")]
public sealed class AuthContextProbeController : ControllerBase
{
    [Authorize]
    [HttpGet("secure")]
    public ActionResult<AuthContextProbeResponse> GetSecure([FromServices] ITenantContextAccessor tenantContextAccessor)
    {
        return Ok(new AuthContextProbeResponse
        {
            AuthenticationType = User.Identity?.AuthenticationType,
            AuthMethod = ResolveAuthMethod(User),
            ApiKeyId = User.FindFirstValue(ApiAuthenticationClaimTypes.ApiKeyId),
            OwnerType = User.FindFirstValue(ApiAuthenticationClaimTypes.OwnerType),
            OwnerId = User.FindFirstValue(ApiAuthenticationClaimTypes.OwnerId),
            TenantId = tenantContextAccessor.TenantId,
            UserId = ResolveUserId(User)
        });
    }

    private static Guid? ResolveUserId(ClaimsPrincipal principal)
    {
        var candidate = principal.FindFirstValue("sub")
                        ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? principal.FindFirstValue("sid");

        return Guid.TryParse(candidate, out var parsed) ? parsed : null;
    }

    private static string? ResolveAuthMethod(ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ApiAuthenticationClaimTypes.AuthMethod)
            ?? (principal.Identity?.IsAuthenticated == true ? "jwt" : null);
    }

    public sealed record AuthContextProbeResponse
    {
        public string? AuthenticationType { get; init; }

        public string? AuthMethod { get; init; }

        public string? ApiKeyId { get; init; }

        public string? OwnerType { get; init; }

        public string? OwnerId { get; init; }

        public Guid? TenantId { get; init; }

        public Guid? UserId { get; init; }
    }
}

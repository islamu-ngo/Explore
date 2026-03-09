// ABOUTME: Internal Phase 0 probe endpoint for validating authentication plus tenant-resolution flow.
// ABOUTME: Returns the resolved runtime context for integration tests without exposing the endpoint in API docs.

using Asp.Versioning;
using Explore.Application.Authentication;
using Explore.Application.Constants;
using Explore.Application.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[ApiController]
[ApiVersion("0.1")]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/_internal/auth-probe")]
public sealed class AuthContextProbeController : ControllerBase
{
    [Authorize]
    [HttpGet("secure")]
    public ActionResult<AuthContextProbeResponse> GetSecure([FromServices] ITenantContextAccessor tenantContextAccessor, [FromServices] IHostEnvironment hostEnvironment, [FromServices] IConfiguration configuration)
    {
        if (!hostEnvironment.IsEnvironment("Testing") || !configuration.GetValue<bool>("Diagnostics:EnableAuthContextProbe"))
        {
            return NotFound();
        }

        var apiKeyContext = User.TryGetApiKeyPrincipalContext();

        return Ok(new AuthContextProbeResponse
        {
            AuthenticationType = User.Identity?.AuthenticationType,
            AuthMethod = User.GetAuthenticationMethod(),
            ApiKeyId = apiKeyContext?.KeyId,
            OwnerType = apiKeyContext?.OwnerType.ToString(),
            OwnerId = apiKeyContext?.OwnerId.ToString(),
            TenantId = tenantContextAccessor.TenantId,
            UserId = User.GetAuthenticatedUserId()
        });
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

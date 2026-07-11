// ABOUTME: Diagnostics-only endpoint for invalidating admin authority cache during full-process E2E tests.
// ABOUTME: Hidden from API docs and disabled unless an explicit diagnostics flag is enabled.

using System.Security.Claims;
using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.Application.Contracts.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[ApiController]
[ApiVersion("0.1")]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/_internal/admin-cache")]
[EndpointClassification(EndpointClass.Authenticated)]
public sealed class AdminCacheDiagnosticsController : ExploreControllerBase
{
    private static readonly ApiNotFoundProblemDescriptor AdminCacheDiagnosticsNotFoundProblem = new(
        "Admin cache diagnostics not found",
        "Admin cache diagnostics are not enabled in this environment.");

    [Authorize]
    [HttpPost("current-user/snapshot")]
    public async Task<ActionResult<AdminCacheCurrentUserDiagnostics>> SnapshotCurrentUser(
        [FromServices] IMediator mediator,
        [FromServices] IHostEnvironment hostEnvironment,
        [FromServices] IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled(configuration, hostEnvironment))
        {
            return this.ToNotFoundProblem(AdminCacheDiagnosticsNotFoundProblem);
        }

        var providerSubject = ResolveProviderSubject();
        var provider = string.IsNullOrWhiteSpace(providerSubject) ? null : ResolveAuthProvider();
        var providerId = provider is null || providerSubject is null
            ? null
            : ResolveProviderId(providerSubject, provider);
        var resolvedUserId = await ResolveCurrentUserIdAsync(mediator, cancellationToken);

        return Ok(new AdminCacheCurrentUserDiagnostics(
            User.Identity?.AuthenticationType,
            User.FindFirst("internal_user_id")?.Value,
            User.FindFirst("sub")?.Value,
            User.FindFirst("sid")?.Value,
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            provider,
            providerId,
            resolvedUserId));
    }

    [Authorize]
    [HttpPost("users/{userId:guid}/invalidate")]
    public ActionResult InvalidateUser(
        Guid userId,
        [FromServices] IAdminCacheInvalidator adminCacheInvalidator,
        [FromServices] IHostEnvironment hostEnvironment,
        [FromServices] IConfiguration configuration)
    {
        if (!IsEnabled(configuration, hostEnvironment))
        {
            return this.ToNotFoundProblem(AdminCacheDiagnosticsNotFoundProblem);
        }

        adminCacheInvalidator.InvalidateUser(userId);
        return NoContent();
    }

    private static bool IsEnabled(IConfiguration configuration, IHostEnvironment hostEnvironment)
    {
        return configuration.GetValue<bool>("Diagnostics:EnableAdminCacheInvalidation") &&
            (hostEnvironment.IsDevelopment() || hostEnvironment.IsEnvironment("Testing"));
    }

    public sealed record AdminCacheCurrentUserDiagnostics(
        string? AuthenticationType,
        string? InternalUserIdClaim,
        string? SubjectClaim,
        string? SessionIdClaim,
        string? NameIdentifierClaim,
        string? Provider,
        string? ProviderId,
        Guid? ResolvedUserId);
}

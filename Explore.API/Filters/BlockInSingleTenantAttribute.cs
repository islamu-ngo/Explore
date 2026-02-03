// ABOUTME: Authorization filter that blocks endpoints in single-tenant deployment mode.
// ABOUTME: Returns 404 to hide SuperAdmin endpoints from discovery in simplified deployments.

using Explore.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Explore.API.Filters;

/// <summary>
/// Authorization filter that blocks access to endpoints in single-tenant mode.
/// Apply to controllers or actions that should only be available in multi-tenant deployments.
/// Returns 404 (Not Found) to hide the endpoint from discovery.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// [BlockInSingleTenant]
/// [ApiController]
/// public class TenantAdminController : ControllerBase { }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class BlockInSingleTenantAttribute : Attribute, IAuthorizationFilter
{
    /// <summary>
    /// Called early in the filter pipeline to confirm request is authorized.
    /// In single-tenant mode with HideSuperAdminInSingleTenant enabled, returns 404.
    /// </summary>
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var deploymentSettings = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<DeploymentSettings>>().Value;

        // Only block if in single-tenant mode AND hiding is enabled
        if (deploymentSettings.IsSingleTenant && deploymentSettings.HideSuperAdminInSingleTenant)
        {
            // Return 404 to hide the endpoint from discovery
            // Using NotFoundResult instead of ForbidResult to prevent enumeration
            context.Result = new NotFoundResult();
        }
    }
}

/// <summary>
/// Authorization filter that requires multi-tenant mode.
/// Unlike BlockInSingleTenant, this returns 403 Forbidden with an error message.
/// Use when you want to inform the client that the feature is unavailable.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireMultiTenantAttribute : Attribute, IAuthorizationFilter
{
    /// <summary>
    /// Called early in the filter pipeline to confirm request is authorized.
    /// In single-tenant mode, returns 403 Forbidden with explanation.
    /// </summary>
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var deploymentSettings = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<DeploymentSettings>>().Value;

        if (deploymentSettings.IsSingleTenant)
        {
            context.Result = new ObjectResult(new
            {
                error = "MultiTenantRequired",
                message = "This endpoint is only available in multi-tenant deployments."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}

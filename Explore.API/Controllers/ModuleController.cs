// ABOUTME: API controller for module governance and discovery.
// Provides endpoints to list available modules and check tenant capabilities.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Hateoas;
using Explore.Application.Contracts.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

/// <summary>
/// Module governance and discovery API endpoints.
/// Provides information about available modules and tenant capabilities.
/// </summary>
[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
public class ModuleController : ControllerBase
{
    private readonly IModuleService _moduleService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ModuleController> _logger;

    public ModuleController(
        IModuleService moduleService,
        ITenantContext tenantContext,
        ILogger<ModuleController> logger)
    {
        _moduleService = moduleService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all globally available modules.
    /// </summary>
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("available", Name = RouteNames.GetAvailableModules)]
    [EndpointSummary("Get Available Modules")]
    [EndpointDescription("Returns all modules that are globally active in the system. " +
        "Does not filter by tenant - use /enabled for tenant-specific modules.")]
    [ProducesResponseType(typeof(IReadOnlyList<ModuleInfo>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ModuleInfo>>> GetAvailableModules(CancellationToken cancellationToken)
    {
        var modules = await _moduleService.GetAllModulesAsync(cancellationToken);
        return Ok(modules);
    }

    /// <summary>
    /// Get modules enabled for the current tenant.
    /// </summary>
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("enabled", Name = RouteNames.GetEnabledModules)]
    [EndpointSummary("Get Enabled Modules")]
    [EndpointDescription("Returns modules enabled for the current tenant. " +
        "These modules determine which aspects/features are available for events. " +
        "Used by the frontend to drive dynamic form generation.")]
    [ProducesResponseType(typeof(IReadOnlyList<ModuleInfo>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ModuleInfo>>> GetEnabledModules(CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var modules = await _moduleService.GetEnabledModulesAsync(tenantId, cancellationToken);
        return Ok(modules);
    }

    /// <summary>
    /// Check if a specific module is enabled for the current tenant.
    /// </summary>
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("{moduleKey}/enabled", Name = RouteNames.CheckModuleEnabled)]
    [EndpointSummary("Check Module Enabled")]
    [EndpointDescription("Checks if a specific module is enabled for the current tenant. " +
        "Returns a simple boolean response.")]
    [ProducesResponseType(typeof(ModuleEnabledResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ModuleEnabledResponse>> IsModuleEnabled(
        string moduleKey,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var isEnabled = await _moduleService.IsModuleEnabledAsync(tenantId, moduleKey, cancellationToken);

        return Ok(new ModuleEnabledResponse
        {
            ModuleKey = moduleKey,
            IsEnabled = isEnabled
        });
    }

    /// <summary>
    /// Get the wizard schema URL for a module.
    /// </summary>
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("{moduleKey}/schema", Name = RouteNames.GetModuleSchemaUrl)]
    [EndpointSummary("Get Module Schema URL")]
    [EndpointDescription("Returns the wizard schema URL for a module. " +
        "The schema is used to generate dynamic forms for module-specific features.")]
    [ProducesResponseType(typeof(ModuleSchemaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ModuleSchemaResponse>> GetModuleSchemaUrl(
        string moduleKey,
        CancellationToken cancellationToken)
    {
        var schemaUrl = await _moduleService.GetModuleWizardSchemaUrlAsync(moduleKey, cancellationToken);

        if (schemaUrl == null)
        {
            return Problem(
                detail: $"Module '{moduleKey}' not found or has no schema",
                statusCode: StatusCodes.Status404NotFound,
                title: "Resource not found");
        }

        return Ok(new ModuleSchemaResponse
        {
            ModuleKey = moduleKey,
            SchemaUrl = schemaUrl
        });
    }

    /// <summary>
    /// Enable a module for the current tenant (admin only).
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost("{moduleKey}/enable", Name = RouteNames.EnableModule)]
    [EndpointSummary("Enable Module")]
    [EndpointDescription("Enables a module for the current tenant. " +
        "Requires admin privileges.")]
    [ProducesResponseType(typeof(ModuleActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ModuleActionResponse>> EnableModule(
        string moduleKey,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        // Get current user ID for audit
        var userIdClaim = User.FindFirst("sub")?.Value
            ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
            ?? User.FindFirst("sid")?.Value;

        Guid? enabledBy = Guid.TryParse(userIdClaim, out var userId) ? userId : null;

        var success = await _moduleService.EnableModuleAsync(tenantId, moduleKey, enabledBy, cancellationToken);

        if (!success)
        {
            return Problem(
                detail: $"Module '{moduleKey}' not found or not active",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad request");
        }

        return Ok(new ModuleActionResponse
        {
            ModuleKey = moduleKey,
            Action = "enabled",
            Success = true
        });
    }

    /// <summary>
    /// Disable a module for the current tenant (admin only).
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost("{moduleKey}/disable", Name = RouteNames.DisableModule)]
    [EndpointSummary("Disable Module")]
    [EndpointDescription("Disables a module for the current tenant. " +
        "Existing data using this module's features will be preserved but hidden. " +
        "Requires admin privileges.")]
    [ProducesResponseType(typeof(ModuleActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ModuleActionResponse>> DisableModule(
        string moduleKey,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var success = await _moduleService.DisableModuleAsync(tenantId, moduleKey, cancellationToken);

        if (!success)
        {
            return Problem(
                detail: $"Module '{moduleKey}' is not enabled for this tenant",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad request");
        }

        return Ok(new ModuleActionResponse
        {
            ModuleKey = moduleKey,
            Action = "disabled",
            Success = true
        });
    }
}

/// <summary>
/// Response for module enabled check.
/// </summary>
public class ModuleEnabledResponse
{
    public string ModuleKey { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
}

/// <summary>
/// Response for module schema URL.
/// </summary>
public class ModuleSchemaResponse
{
    public string ModuleKey { get; init; } = string.Empty;
    public string SchemaUrl { get; init; } = string.Empty;
}

/// <summary>
/// Response for module enable/disable actions.
/// </summary>
public class ModuleActionResponse
{
    public string ModuleKey { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public bool Success { get; init; }
}

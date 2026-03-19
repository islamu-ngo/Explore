// ABOUTME: Admin controller for instance-level settings management via focused sub-resource endpoints.
// ABOUTME: Replaces monolithic GET/PUT settings endpoints with per-domain REST sub-resources.

using System.Security.Claims;
using Asp.Versioning;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Analytics;
using Explore.Application.DTOs.Instance;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Features.InstanceOnboarding.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/instance/settings")]
[ApiController]
[Authorize]
public class InstanceSettingsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAdminContext _adminContext;

    public InstanceSettingsController(IMediator mediator, IAdminContext adminContext)
    {
        _mediator = mediator;
        _adminContext = adminContext;
    }

    // ── Governance Sub-Resource Endpoints ──────────────────────────────

    [HttpGet("modules")]
    [EndpointSummary("Get Module Settings")]
    [EndpointDescription("Returns instance module enablement flags.")]
    [ProducesResponseType(typeof(ModuleSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ModuleSettingsDto>> GetModuleSettings(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdmin(cancellationToken)) return Forbid();
        var settings = await _mediator.Send(new GetInstanceGovernanceSettingsQuery(), cancellationToken);
        return Ok(settings.Modules);
    }

    [HttpPut("modules")]
    [EndpointSummary("Update Module Settings")]
    [EndpointDescription("Updates instance module enablement flags. Requires instance administrator.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateModuleSettings(
        [FromBody] ModuleSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        var response = await _mediator.Send(new UpdateModuleSettingsCommand { UserId = userId.Value, Settings = settings }, cancellationToken);
        return HandleCommandResponse(response);
    }

    [HttpGet("events")]
    [EndpointSummary("Get Event Policy")]
    [EndpointDescription("Returns instance event lifecycle policy settings.")]
    [ProducesResponseType(typeof(EventPolicyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventPolicyDto>> GetEventPolicy(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdmin(cancellationToken)) return Forbid();
        var settings = await _mediator.Send(new GetInstanceGovernanceSettingsQuery(), cancellationToken);
        return Ok(settings.EventPolicy);
    }

    [HttpPut("events")]
    [EndpointSummary("Update Event Policy")]
    [EndpointDescription("Updates instance event lifecycle policy. Requires instance administrator.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateEventPolicy(
        [FromBody] EventPolicyDto settings, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        var response = await _mediator.Send(new UpdateEventPolicyCommand { UserId = userId.Value, Settings = settings }, cancellationToken);
        return HandleCommandResponse(response);
    }

    [HttpGet("organizations")]
    [EndpointSummary("Get Organization Policy")]
    [EndpointDescription("Returns instance organization registration policy settings.")]
    [ProducesResponseType(typeof(OrganizationPolicyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OrganizationPolicyDto>> GetOrganizationPolicy(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdmin(cancellationToken)) return Forbid();
        var settings = await _mediator.Send(new GetInstanceGovernanceSettingsQuery(), cancellationToken);
        return Ok(settings.OrganizationPolicy);
    }

    [HttpPut("organizations")]
    [EndpointSummary("Update Organization Policy")]
    [EndpointDescription("Updates instance organization policy. Requires instance administrator.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateOrganizationPolicy(
        [FromBody] OrganizationPolicyDto settings, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        var response = await _mediator.Send(new UpdateOrganizationPolicyCommand { UserId = userId.Value, Settings = settings }, cancellationToken);
        return HandleCommandResponse(response);
    }

    [HttpGet("branding")]
    [EndpointSummary("Get Branding Settings")]
    [EndpointDescription("Returns instance branding and identity settings.")]
    [ProducesResponseType(typeof(BrandingSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BrandingSettingsDto>> GetBrandingSettings(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdmin(cancellationToken)) return Forbid();
        var settings = await _mediator.Send(new GetInstanceGovernanceSettingsQuery(), cancellationToken);
        return Ok(settings.Branding);
    }

    [HttpPut("branding")]
    [EndpointSummary("Update Branding Settings")]
    [EndpointDescription("Updates instance branding settings. Requires instance administrator.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateBrandingSettings(
        [FromBody] BrandingSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        var response = await _mediator.Send(new UpdateBrandingSettingsCommand { UserId = userId.Value, Settings = settings }, cancellationToken);
        return HandleCommandResponse(response);
    }

    [HttpGet("domains")]
    [EndpointSummary("Get Domain Settings")]
    [EndpointDescription("Returns instance domain and auth provider settings.")]
    [ProducesResponseType(typeof(DomainSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DomainSettingsDto>> GetDomainSettings(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdmin(cancellationToken)) return Forbid();
        var settings = await _mediator.Send(new GetInstanceGovernanceSettingsQuery(), cancellationToken);
        return Ok(settings.Domains);
    }

    [HttpPut("domains")]
    [EndpointSummary("Update Domain Settings")]
    [EndpointDescription("Updates instance domain settings. Requires instance administrator.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateDomainSettings(
        [FromBody] DomainSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        var response = await _mediator.Send(new UpdateDomainSettingsCommand { UserId = userId.Value, Settings = settings }, cancellationToken);
        return HandleCommandResponse(response);
    }

    [HttpGet("tenant-delegation")]
    [EndpointSummary("Get Tenant Delegation Settings")]
    [EndpointDescription("Returns instance tenant delegation and override lock settings.")]
    [ProducesResponseType(typeof(TenantDelegationSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TenantDelegationSettingsDto>> GetTenantDelegationSettings(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdmin(cancellationToken)) return Forbid();
        var settings = await _mediator.Send(new GetInstanceGovernanceSettingsQuery(), cancellationToken);
        return Ok(settings.TenantDelegation);
    }

    [HttpPut("tenant-delegation")]
    [EndpointSummary("Update Tenant Delegation Settings")]
    [EndpointDescription("Updates instance tenant delegation settings. Requires instance administrator.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateTenantDelegationSettings(
        [FromBody] TenantDelegationSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        var response = await _mediator.Send(new UpdateTenantDelegationSettingsCommand { UserId = userId.Value, Settings = settings }, cancellationToken);
        return HandleCommandResponse(response);
    }

    [HttpGet("render-policy")]
    [EndpointSummary("Get Render Policy Settings")]
    [EndpointDescription("Returns instance render policy and UI mode settings.")]
    [ProducesResponseType(typeof(RenderPolicySettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RenderPolicySettingsDto>> GetRenderPolicySettings(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdmin(cancellationToken)) return Forbid();
        var settings = await _mediator.Send(new GetInstanceGovernanceSettingsQuery(), cancellationToken);
        return Ok(settings.RenderPolicy);
    }

    [HttpPut("render-policy")]
    [EndpointSummary("Update Render Policy Settings")]
    [EndpointDescription("Updates instance render policy settings. Requires instance administrator.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateRenderPolicySettings(
        [FromBody] RenderPolicySettingsDto settings, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        var response = await _mediator.Send(new UpdateRenderPolicySettingsCommand { UserId = userId.Value, Settings = settings }, cancellationToken);
        return HandleCommandResponse(response);
    }

    [HttpGet("deployment-mode")]
    [EndpointSummary("Get Deployment Mode")]
    [EndpointDescription("Returns the current instance deployment mode.")]
    [ProducesResponseType(typeof(DeploymentModeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DeploymentModeDto>> GetDeploymentMode(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdmin(cancellationToken)) return Forbid();
        var settings = await _mediator.Send(new GetInstanceGovernanceSettingsQuery(), cancellationToken);
        return Ok(settings.DeploymentMode);
    }

    [HttpPost("deployment-mode")]
    [EndpointSummary("Switch Deployment Mode")]
    [EndpointDescription("Switches the instance between SingleTenant and MultiTenant mode. Requires instance administrator.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateDeploymentMode(
        [FromBody] UpdateDeploymentModeRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        var existingSettings = await _mediator.Send(new GetInstanceGovernanceSettingsQuery(), cancellationToken);

        if (!Enum.TryParse<Domain.Enums.DeploymentMode>(request.DeploymentMode, true, out var parsedMode))
        {
            return BadRequest(new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = "Invalid deployment mode.",
                Errors = new List<string> { "DeploymentMode must be SingleTenant or MultiTenant." }
            });
        }

        var settings = new InstanceGovernanceSettings
        {
            DeploymentMode = new DeploymentModeDto { Mode = parsedMode },
            Modules = existingSettings.Modules,
            EventPolicy = existingSettings.EventPolicy,
            OrganizationPolicy = existingSettings.OrganizationPolicy,
            Branding = existingSettings.Branding,
            Domains = existingSettings.Domains,
            TenantDelegation = existingSettings.TenantDelegation,
            RenderPolicy = existingSettings.RenderPolicy
        };

        var command = new UpdateInstanceGovernanceSettingsCommand
        {
            UserId = userId.Value,
            Settings = settings
        };

        var response = await _mediator.Send(command, cancellationToken);
        return HandleCommandResponse(response);
    }

    // ── Infrastructure Settings (Storage, SMTP, Auth) ──────────────────

    [HttpGet("storage")]
    [EndpointSummary("Get Instance Storage Settings")]
    [EndpointDescription("Returns instance S3 storage settings. Only instance admins can access.")]
    [ProducesResponseType(typeof(InstanceStorageSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<InstanceStorageSettingsDto>> GetStorageSettings(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdmin(cancellationToken)) return Forbid();
        var settings = await _mediator.Send(new GetInstanceStorageSettingsQuery(), cancellationToken);
        return Ok(settings);
    }

    [HttpPut("storage")]
    [EndpointSummary("Update Instance Storage Settings")]
    [EndpointDescription("Updates instance S3 storage settings. Requires instance administrator.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateStorageSettings(
        [FromBody] InstanceStorageSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        var response = await _mediator.Send(new UpdateInstanceStorageSettingsCommand { UserId = userId.Value, Settings = settings }, cancellationToken);
        return HandleCommandResponse(response);
    }

    [HttpPost("storage/test")]
    [EndpointSummary("Test Storage Connection")]
    [EndpointDescription("Tests the S3 storage connection using current settings.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> TestStorageConnection(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdmin(cancellationToken)) return Forbid();

        var storageService = HttpContext.RequestServices.GetRequiredService<Explore.Application.Contracts.Infrastructure.IObjectStorageService>();
        var success = await storageService.TestConnectionAsync(cancellationToken);
        return Ok(new { success, message = success ? "Connection successful." : "Connection failed. Please verify your S3 settings." });
    }

    [HttpGet("smtp")]
    [EndpointSummary("Get Instance SMTP Settings")]
    [EndpointDescription("Returns instance SMTP settings. Only instance admins can access.")]
    [ProducesResponseType(typeof(InstanceSmtpSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<InstanceSmtpSettingsDto>> GetSmtpSettings(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdmin(cancellationToken)) return Forbid();
        var settings = await _mediator.Send(new GetInstanceSmtpSettingsQuery(), cancellationToken);
        return Ok(settings);
    }

    [HttpPut("smtp")]
    [EndpointSummary("Update Instance SMTP Settings")]
    [EndpointDescription("Updates instance SMTP settings. Requires instance administrator.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateSmtpSettings(
        [FromBody] InstanceSmtpSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        var response = await _mediator.Send(new UpdateInstanceSmtpSettingsCommand { UserId = userId.Value, Settings = settings }, cancellationToken);
        return HandleCommandResponse(response);
    }

    [HttpPost("smtp/test")]
    [EndpointSummary("Test SMTP Connection")]
    [EndpointDescription("Tests the SMTP connection using current settings.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> TestSmtpConnection(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdmin(cancellationToken)) return Forbid();

        var emailService = HttpContext.RequestServices.GetRequiredService<Explore.Application.Contracts.Infrastructure.IEmailService>();
        var result = await emailService.TestConnectionAsync(cancellationToken);

        var message = result.Success
            ? (string.IsNullOrWhiteSpace(result.Message) ? "Connection successful." : result.Message)
            : (string.IsNullOrWhiteSpace(result.ErrorMessage) ? "Connection failed. Please verify your SMTP settings." : result.ErrorMessage);

        return Ok(new { success = result.Success, message });
    }

    [HttpGet("resolver-config")]
    [EndpointSummary("Get Tenant Resolver Configuration")]
    [EndpointDescription("Returns instance-level tenant resolver configuration.")]
    [ProducesResponseType(typeof(ResolverConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResolverConfigurationDto>> GetResolverConfiguration(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdmin(cancellationToken)) return Forbid();
        var configuration = await _mediator.Send(new GetResolverConfigurationQuery(), cancellationToken);
        return Ok(configuration);
    }

    [HttpPut("resolver-config")]
    [EndpointSummary("Update Tenant Resolver Configuration")]
    [EndpointDescription("Updates instance-level tenant resolver configuration. Requires instance administrator.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateResolverConfiguration(
        [FromBody] ResolverConfigurationDto configuration, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        var response = await _mediator.Send(new UpdateResolverConfigurationCommand { UserId = userId.Value, Configuration = configuration }, cancellationToken);
        return HandleCommandResponse(response);
    }

    [HttpGet("analytics-governance")]
    [EndpointSummary("Get Analytics Governance Settings")]
    [EndpointDescription("Returns analytics and cookie consent governance settings.")]
    [ProducesResponseType(typeof(AnalyticsGovernanceSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AnalyticsGovernanceSettingsDto>> GetAnalyticsGovernanceSettings(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdmin(cancellationToken)) return Forbid();
        var settings = await _mediator.Send(new GetAnalyticsGovernanceSettingsQuery(), cancellationToken);
        return Ok(settings);
    }

    [HttpPut("analytics-governance")]
    [EndpointSummary("Update Analytics Governance Settings")]
    [EndpointDescription("Updates analytics and cookie consent governance settings. Requires instance administrator.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateAnalyticsGovernanceSettings(
        [FromBody] AnalyticsGovernanceSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        var response = await _mediator.Send(new UpdateAnalyticsGovernanceSettingsCommand { UserId = userId.Value, Settings = settings }, cancellationToken);
        return HandleCommandResponse(response);
    }

    [HttpGet("auth-provider")]
    [EndpointSummary("Get Auth Provider Configuration")]
    [EndpointDescription("Returns current auth provider configuration. Secrets are redacted.")]
    [ProducesResponseType(typeof(AuthProviderConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuthProviderConfigurationDto>> GetAuthProviderConfiguration(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdmin(cancellationToken)) return Forbid();
        var configuration = await _mediator.Send(new GetAuthProviderConfigurationQuery(), cancellationToken);
        return Ok(configuration);
    }

    [HttpPut("auth-provider")]
    [EndpointSummary("Update Auth Provider Configuration")]
    [EndpointDescription("Updates auth provider configuration. Requires instance administrator and blocks self-lockout.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateAuthProviderConfiguration(
        [FromBody] AuthProviderConfigurationDto configuration, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        var response = await _mediator.Send(new UpdateAuthProviderConfigurationCommand { UserId = userId.Value, Configuration = configuration }, cancellationToken);
        return HandleCommandResponse(response);
    }

    [HttpGet("auth-provider/status")]
    [AllowAnonymous]
    [EndpointSummary("Check Auth Provider Configuration Status")]
    [EndpointDescription("Returns whether any auth provider has been configured.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> IsAuthProviderConfigured(CancellationToken cancellationToken = default)
    {
        var service = HttpContext.RequestServices.GetRequiredService<IAuthProviderConfigurationService>();
        var isConfigured = await service.IsConfiguredAsync();
        return Ok(new { configured = isConfigured });
    }

    // ── Helpers ────────────────────────────────────────────────────────

    private async Task<bool> IsInstanceAdmin(CancellationToken cancellationToken)
    {
        return await _adminContext.IsInstanceAdminAsync(cancellationToken);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst("internal_user_id")?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sid")?.Value;

        return Guid.TryParse(claim, out var parsedUserId) ? parsedUserId : null;
    }

    private static BaseCommandResponse<Guid> InvalidIdentityResponse() => new()
    {
        Success = false,
        Message = "Invalid user identity."
    };

    private ActionResult<BaseCommandResponse<Guid>> HandleCommandResponse(BaseCommandResponse<Guid> response)
    {
        if (response.Success) return Ok(response);

        if (response.Message?.Contains("Only instance administrators", StringComparison.OrdinalIgnoreCase) == true)
            return Forbid();

        return BadRequest(response);
    }
}

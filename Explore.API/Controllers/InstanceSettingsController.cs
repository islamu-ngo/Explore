// ABOUTME: Admin controller for instance-level settings management via focused sub-resource endpoints.
// ABOUTME: Replaces monolithic GET/PUT settings endpoints with per-domain REST sub-resources.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Hateoas;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Analytics;
using Explore.Application.DTOs.Footer;
using Explore.Application.DTOs.Instance;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.Footer.Requests.Commands;
using Explore.Application.Features.Footer.Requests.Queries;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Features.InstanceOnboarding.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/instance/settings")]
[ApiController]
[Authorize]
[EndpointClassification(EndpointClass.Authenticated)]
public class InstanceSettingsController : ExploreControllerBase
{
    private const string SetupSecretHeader = "X-Setup-Secret";

    private readonly IMediator _mediator;
    private readonly IAdminContext _adminContext;
    private readonly ISetupSecretProvider _setupSecretProvider;
    private readonly IDeploymentModeProvider _deploymentModeProvider;
    private readonly IResourceAssembler<InstanceStorageSettingsDto, InstanceStorageSettingsDto> _storageSettingsAssembler;

    public InstanceSettingsController(
        IMediator mediator,
        IAdminContext adminContext,
        ISetupSecretProvider setupSecretProvider,
        IDeploymentModeProvider deploymentModeProvider,
        IResourceAssembler<InstanceStorageSettingsDto, InstanceStorageSettingsDto> storageSettingsAssembler)
    {
        _mediator = mediator;
        _adminContext = adminContext;
        _setupSecretProvider = setupSecretProvider;
        _deploymentModeProvider = deploymentModeProvider;
        _storageSettingsAssembler = storageSettingsAssembler;
    }

    // ── Governance Sub-Resource Endpoints ──────────────────────────────

    [HttpGet("modules", Name = RouteNames.GetInstanceModuleSettings)]
    [EndpointSummary("Get Module Settings")]
    [EndpointDescription("Returns instance module enablement flags.")]
    [ProducesResponseType(typeof(ModuleSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ModuleSettingsDto>> GetModuleSettings(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdminOrSetupAuthenticated(cancellationToken)) return Forbid();
        var settings = await _mediator.Send(new GetInstanceGovernanceSettingsQuery(), cancellationToken);
        return Ok(settings.Modules);
    }

    [HttpPut("modules", Name = RouteNames.UpdateInstanceModuleSettings)]
    [EndpointSummary("Update Module Settings")]
    [EndpointDescription("Updates instance module enablement flags. Requires instance administrator.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateModuleSettings(
        [FromBody] ModuleSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var userId = await ResolveCurrentUserIdAsync(_mediator, cancellationToken);
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        var response = await _mediator.Send(new UpdateModuleSettingsCommand { UserId = userId.Value, Settings = settings }, cancellationToken);
        return HandleCommandResponse(response);
    }

    [HttpGet("events", Name = RouteNames.GetInstanceEventPolicy)]
    [EndpointSummary("Get Event Policy")]
    [EndpointDescription("Returns instance event lifecycle policy settings.")]
    [ProducesResponseType(typeof(EventPolicyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventPolicyDto>> GetEventPolicy(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdminOrSetupAuthenticated(cancellationToken)) return Forbid();
        var settings = await _mediator.Send(new GetInstanceGovernanceSettingsQuery(), cancellationToken);
        return Ok(settings.EventPolicy);
    }

    [HttpPut("events", Name = RouteNames.UpdateInstanceEventPolicy)]
    [EndpointSummary("Update Event Policy")]
    [EndpointDescription("Updates instance event lifecycle policy. Requires instance administrator.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateEventPolicy(
        [FromBody] EventPolicyDto settings, CancellationToken cancellationToken = default)
    {
        var userId = await ResolveCurrentUserIdAsync(_mediator, cancellationToken);
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        var response = await _mediator.Send(new UpdateEventPolicyCommand { UserId = userId.Value, Settings = settings }, cancellationToken);
        return HandleCommandResponse(response);
    }

    [HttpGet("organizations", Name = RouteNames.GetInstanceOrganizationPolicy)]
    [EndpointSummary("Get Organization Policy")]
    [EndpointDescription("Returns instance organization registration policy settings.")]
    [ProducesResponseType(typeof(OrganizationPolicyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OrganizationPolicyDto>> GetOrganizationPolicy(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdminOrSetupAuthenticated(cancellationToken)) return Forbid();
        var settings = await _mediator.Send(new GetInstanceGovernanceSettingsQuery(), cancellationToken);
        return Ok(settings.OrganizationPolicy);
    }

    [HttpPut("organizations", Name = RouteNames.UpdateInstanceOrganizationPolicy)]
    [EndpointSummary("Update Organization Policy")]
    [EndpointDescription("Updates instance organization policy. Requires instance administrator.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateOrganizationPolicy(
        [FromBody] OrganizationPolicyDto settings, CancellationToken cancellationToken = default)
    {
        var userId = await ResolveCurrentUserIdAsync(_mediator, cancellationToken);
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        var response = await _mediator.Send(new UpdateOrganizationPolicyCommand { UserId = userId.Value, Settings = settings }, cancellationToken);
        return HandleCommandResponse(response);
    }

    [HttpGet("branding", Name = RouteNames.GetInstanceBrandingSettings)]
    [EndpointSummary("Get Branding Settings")]
    [EndpointDescription("Returns instance branding and identity settings.")]
    [ProducesResponseType(typeof(BrandingSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BrandingSettingsDto>> GetBrandingSettings(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdminOrSetupAuthenticated(cancellationToken)) return Forbid();
        var settings = await _mediator.Send(new GetInstanceGovernanceSettingsQuery(), cancellationToken);
        return Ok(settings.Branding);
    }

    [HttpPut("branding", Name = RouteNames.UpdateInstanceBrandingSettings)]
    [EndpointSummary("Update Branding Settings")]
    [EndpointDescription("Updates instance branding settings. Requires instance administrator.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateBrandingSettings(
        [FromBody] BrandingSettingsDto settings,
        [FromServices] IOutputCacheStore cacheStore,
        CancellationToken cancellationToken = default)
    {
        var userId = await ResolveCurrentUserIdAsync(_mediator, cancellationToken);
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        var response = await _mediator.Send(new UpdateBrandingSettingsCommand { UserId = userId.Value, Settings = settings }, cancellationToken);
        if (response.Success)
        {
            await cacheStore.EvictByTagAsync("public-experience-shell", cancellationToken);
        }

        return HandleCommandResponse(response);
    }

    [HttpGet("domains", Name = RouteNames.GetInstanceDomainSettings)]
    [EndpointSummary("Get Domain Settings")]
    [EndpointDescription("Returns instance domain and auth provider settings.")]
    [ProducesResponseType(typeof(DomainSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DomainSettingsDto>> GetDomainSettings(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdminOrSetupAuthenticated(cancellationToken)) return Forbid();
        var settings = await _mediator.Send(new GetInstanceGovernanceSettingsQuery(), cancellationToken);
        return Ok(settings.Domains);
    }

    [HttpPut("domains", Name = RouteNames.UpdateInstanceDomainSettings)]
    [EndpointSummary("Update Domain Settings")]
    [EndpointDescription("Updates instance domain settings. Requires instance administrator.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateDomainSettings(
        [FromBody] DomainSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var userId = await ResolveCurrentUserIdAsync(_mediator, cancellationToken);
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        var response = await _mediator.Send(new UpdateDomainSettingsCommand { UserId = userId.Value, Settings = settings }, cancellationToken);
        return HandleCommandResponse(response);
    }

    [HttpGet("tenant-delegation", Name = RouteNames.GetInstanceTenantDelegationSettings)]
    [EndpointSummary("Get Tenant Delegation Settings")]
    [EndpointDescription("Returns instance tenant delegation and override lock settings.")]
    [ProducesResponseType(typeof(TenantDelegationSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TenantDelegationSettingsDto>> GetTenantDelegationSettings(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdminOrSetupAuthenticated(cancellationToken)) return Forbid();
        var settings = await _mediator.Send(new GetInstanceGovernanceSettingsQuery(), cancellationToken);
        return Ok(settings.TenantDelegation);
    }

    [HttpPut("tenant-delegation", Name = RouteNames.UpdateInstanceTenantDelegationSettings)]
    [EndpointSummary("Update Tenant Delegation Settings")]
    [EndpointDescription("Updates instance tenant delegation settings. Requires instance administrator.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateTenantDelegationSettings(
        [FromBody] TenantDelegationSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var userId = await ResolveCurrentUserIdAsync(_mediator, cancellationToken);
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        var response = await _mediator.Send(new UpdateTenantDelegationSettingsCommand { UserId = userId.Value, Settings = settings }, cancellationToken);
        return HandleCommandResponse(response);
    }

    [HttpGet("render-policy", Name = RouteNames.GetInstanceRenderPolicySettings)]
    [EndpointSummary("Get Render Policy Settings")]
    [EndpointDescription("Returns instance render policy and UI mode settings.")]
    [ProducesResponseType(typeof(RenderPolicySettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RenderPolicySettingsDto>> GetRenderPolicySettings(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdminOrSetupAuthenticated(cancellationToken)) return Forbid();
        var settings = await _mediator.Send(new GetInstanceGovernanceSettingsQuery(), cancellationToken);
        return Ok(settings.RenderPolicy);
    }

    [HttpPut("render-policy", Name = RouteNames.UpdateInstanceRenderPolicySettings)]
    [EndpointSummary("Update Render Policy Settings")]
    [EndpointDescription("Updates instance render policy settings. Requires instance administrator.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateRenderPolicySettings(
        [FromBody] RenderPolicySettingsDto settings, CancellationToken cancellationToken = default)
    {
        var userId = await ResolveCurrentUserIdAsync(_mediator, cancellationToken);
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        var response = await _mediator.Send(new UpdateRenderPolicySettingsCommand { UserId = userId.Value, Settings = settings }, cancellationToken);
        return HandleCommandResponse(response);
    }

    [HttpGet("deployment-mode", Name = RouteNames.GetInstanceDeploymentMode)]
    [EndpointSummary("Get Deployment Mode")]
    [EndpointDescription("Returns the current instance deployment mode.")]
    [ProducesResponseType(typeof(DeploymentModeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DeploymentModeDto>> GetDeploymentMode(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdminOrSetupAuthenticated(cancellationToken)) return Forbid();
        var mode = await _deploymentModeProvider.GetCurrentModeAsync(cancellationToken);
        return Ok(new DeploymentModeDto { Mode = mode });
    }

    [HttpPost("deployment-mode", Name = RouteNames.UpdateInstanceDeploymentMode)]
    [EndpointSummary("Deployment Mode Is Operator-Controlled")]
    [EndpointDescription("Runtime deployment mode switching is disabled. Set DEPLOYMENT_MODE before first-run onboarding.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateDeploymentMode(
        [FromBody] UpdateDeploymentModeRequest request, CancellationToken cancellationToken = default)
    {
        var userId = await ResolveCurrentUserIdAsync(_mediator, cancellationToken);
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        _ = request;
        return BadRequest(new BaseCommandResponse<Guid>
        {
            Success = false,
            FailureCode = "DeploymentModeChangeRequiresOperatorConfiguration",
            Message = "Deployment mode is operator-controlled.",
            Errors =
            [
                "Set DEPLOYMENT_MODE before first-run onboarding. Runtime admin switching is disabled."
            ]
        });
    }

    // ── Infrastructure Settings (Storage, SMTP, Auth) ──────────────────

    [HttpGet("storage", Name = RouteNames.GetInstanceStorageSettings)]
    [EndpointSummary("Get Instance Storage Settings")]
    [EndpointDescription("Returns provider policy, quotas, usage, health, and redacted optional S3 settings. Only instance admins can access.")]
    [Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
    [ProducesResponseType(typeof(HalResource<InstanceStorageSettingsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<HalResource<InstanceStorageSettingsDto>>> GetStorageSettings(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdminOrSetupAuthenticated(cancellationToken)) return Forbid();
        var settings = await _mediator.Send(new GetInstanceStorageSettingsQuery(), cancellationToken);
        var halResource = await _storageSettingsAssembler.ToResource(settings, HttpContext);
        return Ok(halResource);
    }

    [HttpPut("storage", Name = RouteNames.UpdateInstanceStorageSettings)]
    [EndpointSummary("Update Instance Storage Settings")]
    [EndpointDescription("Updates instance storage provider policy, quotas, delegation lock, and optional S3 settings. Requires instance administrator.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateStorageSettings(
        [FromBody] InstanceStorageSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var userId = await ResolveCurrentUserIdAsync(_mediator, cancellationToken);
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        var response = await _mediator.Send(new UpdateInstanceStorageSettingsCommand { UserId = userId.Value, Settings = settings }, cancellationToken);
        return HandleCommandResponse(response);
    }

    [HttpPost("storage/test", Name = RouteNames.TestInstanceStorageConnection)]
    [EndpointSummary("Test Storage Connection")]
    [EndpointDescription("Tests the currently selected storage provider using current instance settings.")]
    [ProducesResponseType(typeof(InstanceStorageProviderStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<InstanceStorageProviderStatusDto>> TestStorageConnection(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdminOrSetupAuthenticated(cancellationToken)) return Forbid();

        var status = await _mediator.Send(new TestInstanceStorageProviderQuery(), cancellationToken);
        return Ok(status);
    }

    [HttpPost("storage/usage/recalculate", Name = RouteNames.RecalculateInstanceStorageUsage)]
    [EndpointSummary("Recalculate Storage Usage")]
    [EndpointDescription("Reconciles instance-wide storage usage counters from storage metadata. Requires instance administrator.")]
    [ProducesResponseType(typeof(InstanceStorageUsageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<InstanceStorageUsageDto>> RecalculateStorageUsage(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdminOrSetupAuthenticated(cancellationToken)) return Forbid();

        var usage = await _mediator.Send(new RecalculateInstanceStorageUsageCommand(), cancellationToken);
        return Ok(usage);
    }

    [HttpGet("smtp", Name = RouteNames.GetInstanceSmtpSettings)]
    [EndpointSummary("Get Instance SMTP Settings")]
    [EndpointDescription("Returns instance SMTP settings. Only instance admins can access.")]
    [ProducesResponseType(typeof(InstanceSmtpSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<InstanceSmtpSettingsDto>> GetSmtpSettings(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdminOrSetupAuthenticated(cancellationToken)) return Forbid();
        var settings = await _mediator.Send(new GetInstanceSmtpSettingsQuery(), cancellationToken);
        return Ok(settings);
    }

    [HttpPut("smtp", Name = RouteNames.UpdateInstanceSmtpSettings)]
    [EndpointSummary("Update Instance SMTP Settings")]
    [EndpointDescription("Updates instance SMTP settings. Requires instance administrator.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateSmtpSettings(
        [FromBody] InstanceSmtpSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var userId = await ResolveCurrentUserIdAsync(_mediator, cancellationToken);
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        var response = await _mediator.Send(new UpdateInstanceSmtpSettingsCommand { UserId = userId.Value, Settings = settings }, cancellationToken);
        return HandleCommandResponse(response);
    }

    [HttpPost("smtp/test", Name = RouteNames.TestInstanceSmtpConnection)]
    [EndpointSummary("Test SMTP Connection")]
    [EndpointDescription("Tests the SMTP connection using current settings.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> TestSmtpConnection(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdminOrSetupAuthenticated(cancellationToken)) return Forbid();

        var emailService = HttpContext.RequestServices.GetRequiredService<Explore.Application.Contracts.Infrastructure.IEmailService>();
        var result = await emailService.TestConnectionAsync(cancellationToken);

        var message = result.Success
            ? (string.IsNullOrWhiteSpace(result.Message) ? "Connection successful." : result.Message)
            : (string.IsNullOrWhiteSpace(result.ErrorMessage) ? "Connection failed. Please verify your SMTP settings." : result.ErrorMessage);

        return Ok(new { success = result.Success, message });
    }

    [HttpGet("resolver-config", Name = RouteNames.GetInstanceResolverConfiguration)]
    [EndpointSummary("Get Tenant Resolver Configuration")]
    [EndpointDescription("Returns instance-level tenant resolver configuration.")]
    [ProducesResponseType(typeof(ResolverConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResolverConfigurationDto>> GetResolverConfiguration(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdminOrSetupAuthenticated(cancellationToken)) return Forbid();
        var configuration = await _mediator.Send(new GetResolverConfigurationQuery(), cancellationToken);
        return Ok(configuration);
    }

    [HttpPut("resolver-config", Name = RouteNames.UpdateInstanceResolverConfiguration)]
    [EndpointSummary("Update Tenant Resolver Configuration")]
    [EndpointDescription("Updates instance-level tenant resolver configuration. Requires instance administrator.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateResolverConfiguration(
        [FromBody] ResolverConfigurationDto configuration, CancellationToken cancellationToken = default)
    {
        var userId = await ResolveCurrentUserIdAsync(_mediator, cancellationToken);
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        var response = await _mediator.Send(new UpdateResolverConfigurationCommand { UserId = userId.Value, Configuration = configuration }, cancellationToken);
        return HandleCommandResponse(response);
    }

    [HttpGet("analytics-governance", Name = RouteNames.GetInstanceAnalyticsGovernanceSettings)]
    [EndpointSummary("Get Analytics Governance Settings")]
    [EndpointDescription("Returns analytics and cookie consent governance settings.")]
    [ProducesResponseType(typeof(AnalyticsGovernanceSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AnalyticsGovernanceSettingsDto>> GetAnalyticsGovernanceSettings(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdminOrSetupAuthenticated(cancellationToken)) return Forbid();
        var settings = await _mediator.Send(new GetAnalyticsGovernanceSettingsQuery(), cancellationToken);
        return Ok(settings);
    }

    [HttpPut("analytics-governance", Name = RouteNames.UpdateInstanceAnalyticsGovernanceSettings)]
    [EndpointSummary("Update Analytics Governance Settings")]
    [EndpointDescription("Updates analytics and cookie consent governance settings. Requires instance administrator.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateAnalyticsGovernanceSettings(
        [FromBody] AnalyticsGovernanceSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var userId = await ResolveCurrentUserIdAsync(_mediator, cancellationToken);
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        var response = await _mediator.Send(new UpdateAnalyticsGovernanceSettingsCommand { UserId = userId.Value, Settings = settings }, cancellationToken);
        return HandleCommandResponse(response);
    }

    [HttpGet("footer-governance", Name = RouteNames.GetFooterGovernanceSettings)]
    [EndpointSummary("Get Footer Governance Settings")]
    [EndpointDescription("Returns instance-level footer lock flags. Requires instance administrator.")]
    [ProducesResponseType(typeof(FooterGovernanceSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FooterGovernanceSettingsDto>> GetFooterGovernanceSettings(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdminOrSetupAuthenticated(cancellationToken)) return Forbid();
        var settings = await _mediator.Send(new GetFooterGovernanceSettingsQuery(), cancellationToken);
        return Ok(settings);
    }

    [HttpPut("footer-governance", Name = RouteNames.UpdateFooterGovernanceSettings)]
    [EndpointSummary("Update Footer Governance Settings")]
    [EndpointDescription("Updates instance-level footer lock flags. Requires instance administrator.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateFooterGovernanceSettings(
        [FromBody] FooterGovernanceSettingsDto settings, CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdminOrSetupAuthenticated(cancellationToken)) return Forbid();
        var userId = await ResolveCurrentUserIdAsync(_mediator, cancellationToken);
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        var response = await _mediator.Send(new UpdateFooterGovernanceSettingsCommand { UserId = userId.Value, Settings = settings }, cancellationToken);
        return HandleCommandResponse(response);
    }

    [HttpGet("auth-provider", Name = RouteNames.GetInstanceAuthProviderConfiguration)]
    [EndpointSummary("Get Auth Provider Configuration")]
    [EndpointDescription("Returns current auth provider configuration. Secrets are redacted.")]
    [ProducesResponseType(typeof(AuthProviderConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuthProviderConfigurationDto>> GetAuthProviderConfiguration(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdminOrSetupAuthenticated(cancellationToken)) return Forbid();
        var configuration = await _mediator.Send(new GetAuthProviderConfigurationQuery(), cancellationToken);
        return Ok(configuration);
    }

    [HttpPut("auth-provider", Name = RouteNames.UpdateInstanceAuthProviderConfiguration)]
    [EndpointSummary("Update Auth Provider Configuration")]
    [EndpointDescription("Updates auth provider configuration. Requires instance administrator and blocks self-lockout.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateAuthProviderConfiguration(
        [FromBody] AuthProviderConfigurationDto configuration, CancellationToken cancellationToken = default)
    {
        var userId = await ResolveCurrentUserIdAsync(_mediator, cancellationToken);
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        var response = await _mediator.Send(new UpdateAuthProviderConfigurationCommand { UserId = userId.Value, Configuration = configuration }, cancellationToken);
        return HandleCommandResponse(response);
    }

    [HttpPost("auth-provider/keycloak/doctor", Name = RouteNames.RunInstanceKeycloakRealmDoctor)]
    [EndpointSummary("Run Keycloak Realm Doctor")]
    [EndpointDescription("Runs read-only Keycloak realm diagnostics. Temporary admin credentials are used only for this request and are not stored.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(KeycloakRealmDoctorResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<KeycloakRealmDoctorResultDto>> RunKeycloakRealmDoctor(
        [FromBody] KeycloakRealmDoctorRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdminOrSetupAuthenticated(cancellationToken)) return Forbid();

        var result = await _mediator.Send(new RunKeycloakRealmDoctorQuery { Request = request }, cancellationToken);
        return Ok(result);
    }

    [HttpPost("auth-provider/keycloak/sync-preview", Name = RouteNames.PreviewInstanceKeycloakRealmSync)]
    [EndpointSummary("Preview Keycloak Realm Sync")]
    [EndpointDescription("Generates a read-only additive Keycloak realm sync plan. Temporary admin credentials are used only for this request and are not stored.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(KeycloakRealmSyncPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<KeycloakRealmSyncPlanDto>> PreviewKeycloakRealmSync(
        [FromBody] KeycloakRealmSyncPreviewRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdminOrSetupAuthenticated(cancellationToken)) return Forbid();

        var result = await _mediator.Send(new PreviewKeycloakRealmSyncQuery { Request = request }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("auth-provider/status", Name = RouteNames.GetInstanceAuthProviderConfigurationStatus)]
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

    [HttpGet("authz-provider", Name = RouteNames.GetInstanceAuthorizationProviderConfiguration)]
    [EndpointSummary("Get Authorization Provider Configuration")]
    [EndpointDescription("Returns current authorization provider configuration for instance administration.")]
    [ProducesResponseType(typeof(AuthorizationProviderConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuthorizationProviderConfigurationDto>> GetAuthorizationProviderConfiguration(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdminOrSetupAuthenticated(cancellationToken)) return Forbid();
        var configuration = await _mediator.Send(new GetAuthorizationProviderConfigurationQuery(), cancellationToken);
        return Ok(configuration);
    }

    [HttpPut("authz-provider", Name = RouteNames.UpdateInstanceAuthorizationProviderConfiguration)]
    [EndpointSummary("Update Authorization Provider Configuration")]
    [EndpointDescription("Updates authorization provider configuration. Requires instance administrator.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateAuthorizationProviderConfiguration(
        [FromBody] AuthorizationProviderConfigurationDto configuration, CancellationToken cancellationToken = default)
    {
        var userId = await ResolveCurrentUserIdAsync(_mediator, cancellationToken);
        if (!userId.HasValue) return BadRequest(InvalidIdentityResponse());

        var response = await _mediator.Send(
            new UpdateAuthorizationProviderConfigurationCommand { UserId = userId.Value, Configuration = configuration },
            cancellationToken);

        return HandleCommandResponse(response);
    }

    [HttpPost("authz-provider/sync", Name = RouteNames.SyncInstanceAuthorizationPolicyPackage)]
    [EndpointSummary("Sync Authorization Policy Package")]
    [EndpointDescription("Publishes the authorization policy package. Requires instance administrator.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> SyncAuthorizationPolicyPackage(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdminOrSetupAuthenticated(cancellationToken)) return Forbid();

        var response = await _mediator.Send(new SyncAuthorizationPolicyPackageCommand(), cancellationToken);
        return HandleCommandResponse(response);
    }

    [HttpGet("authz-provider/package", Name = RouteNames.DownloadInstanceAuthorizationPolicyPackage)]
    [EndpointSummary("Download Authorization Policy Package")]
    [EndpointDescription("Downloads a ZIP archive containing the authorization policy package and manual cerbosctl instructions. Requires instance administrator.")]
    [Produces("application/zip")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DownloadAuthorizationPolicyPackage(CancellationToken cancellationToken = default)
    {
        if (!await IsInstanceAdminOrSetupAuthenticated(cancellationToken)) return Forbid();

        try
        {
            var archive = await _mediator.Send(new DownloadAuthorizationPolicyPackageQuery(), cancellationToken);
            return File(archive.Content, archive.ContentType, archive.FileName);
        }
        catch (PolicyPackageUnavailableException)
        {
            return AuthorizationPolicyPackageUnavailableProblem();
        }
    }

    [HttpGet("authz-provider/status", Name = RouteNames.GetInstanceAuthorizationProviderConfigurationStatus)]
    [AllowAnonymous]
    [EndpointSummary("Check Authorization Provider Configuration Status")]
    [EndpointDescription("Returns whether an authorization provider has been configured.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> IsAuthorizationProviderConfigured(CancellationToken cancellationToken = default)
    {
        var service = HttpContext.RequestServices.GetRequiredService<IAuthorizationProviderConfigurationService>();
        var isConfigured = await service.IsConfiguredAsync();
        return Ok(new { configured = isConfigured });
    }

    private ObjectResult AuthorizationPolicyPackageUnavailableProblem() =>
        Problem(
            title: "Authorization policy package unavailable",
            detail: "The bundled Cerbos policy package is not available to this API deployment. Mount or bundle the package directory and retry the download.",
            statusCode: StatusCodes.Status503ServiceUnavailable);

    // ── Helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Reads instance settings are allowed for real instance administrators OR for any
    /// authenticated caller presenting a valid setup secret while onboarding is still active.
    /// This allows the onboarding wizard to pre-populate form fields from existing settings
    /// before the first instance admin has been assigned.
    /// </summary>
    private async Task<bool> IsInstanceAdminOrSetupAuthenticated(CancellationToken cancellationToken)
    {
        if (await _adminContext.IsInstanceAdminAsync(cancellationToken))
            return true;

        if (!_setupSecretProvider.IsSetupModeActive)
            return false;

        var setupSecret = Request.Headers.TryGetValue(SetupSecretHeader, out var value)
            ? value.ToString()
            : null;

        return !string.IsNullOrEmpty(setupSecret) && _setupSecretProvider.ValidateSecret(setupSecret);
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

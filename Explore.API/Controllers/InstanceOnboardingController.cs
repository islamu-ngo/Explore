// ABOUTME: API controller for first-run instance onboarding wizard (one-time setup flow).
// ABOUTME: Provides status check, onboarding completion, secret validation, and setup-time auth provider config.

using System.Security.Claims;
using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Filters;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Features.InstanceOnboarding.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
public class InstanceOnboardingController : ExploreControllerBase
{
    private readonly IMediator _mediator;
    private readonly ISetupSecretProvider _setupSecretProvider;
    private readonly ILogger<InstanceOnboardingController> _logger;

    public InstanceOnboardingController(
        IMediator mediator,
        ISetupSecretProvider setupSecretProvider,
        ILogger<InstanceOnboardingController> logger)
    {
        _mediator = mediator;
        _setupSecretProvider = setupSecretProvider;
        _logger = logger;
    }

    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("status")]
    [EndpointSummary("Get Instance Onboarding Status")]
    [EndpointDescription("Returns whether first-run onboarding is completed and whether the current user is instance admin.")]
    [ProducesResponseType(typeof(InstanceOnboardingStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstanceOnboardingStatusDto>> GetStatus(CancellationToken cancellationToken = default)
    {
        var status = await _mediator.Send(new GetInstanceOnboardingStatusQuery(), cancellationToken);
        return Ok(status);
    }

    [Authorize]
    [SetupSecretRequired]
    [EndpointClassification(EndpointClass.Admin)]
    [HttpPost("complete")]
    [EndpointSummary("Complete Instance Onboarding")]
    [EndpointDescription("Completes first-run onboarding, assigns the current user as instance admin, and persists deployment mode.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Complete([FromBody] CompleteInstanceOnboardingRequest settings, CancellationToken cancellationToken = default)
    {
        var currentUserId = CurrentUserId;
        if (!currentUserId.HasValue)
        {
            return BadRequest(new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = "Invalid user identity."
            });
        }

        var command = new CompleteInstanceOnboardingCommand
        {
            UserId = currentUserId.Value,
            Settings = settings,
            Email = User.FindFirst("email")?.Value
                ?? User.FindFirst(ClaimTypes.Email)?.Value,
            FirstName = User.FindFirst("given_name")?.Value
                ?? User.FindFirst(ClaimTypes.GivenName)?.Value,
            LastName = User.FindFirst("family_name")?.Value
                ?? User.FindFirst(ClaimTypes.Surname)?.Value,
            Username = User.FindFirst("preferred_username")?.Value
                ?? User.FindFirst(ClaimTypes.Name)?.Value,
            AuthProvider = User.FindFirst("idp")?.Value
                ?? User.Identity?.AuthenticationType,
            AuthProviderId = User.FindFirst("sub")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            EmailVerified = bool.TryParse(User.FindFirst("email_verified")?.Value, out var ev) ? ev : null
        };

        var response = await _mediator.Send(command, cancellationToken);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        _logger.LogWarning(
            "Instance claimed by admin (userId: {UserId}) from IP: {IpAddress}. Bootstrap mode disabled.",
            currentUserId, HttpContext.Connection.RemoteIpAddress);

        return Ok(response);
    }

    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpPost("validate-secret")]
    [EnableRateLimiting("SetupSecret")]
    [EndpointSummary("Validate Setup Secret")]
    [EndpointDescription("Validates the provided setup secret. Returns whether the secret is correct. Rate limited to 5 attempts per minute.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status410Gone)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public ActionResult ValidateSecret([FromBody] ValidateSetupSecretRequest request)
    {
        if (!_setupSecretProvider.IsSetupModeActive)
            return StatusCode(StatusCodes.Status410Gone, new { valid = false, error = "Setup already completed." });

        var isValid = _setupSecretProvider.ValidateSecret(request.Secret);
        return Ok(new { valid = isValid });
    }

    [AllowAnonymous]
    [SetupSecretRequired]
    [EndpointClassification(EndpointClass.Admin)]
    [HttpGet("auth-provider-configuration/internal")]
    [EndpointSummary("Get Auth Provider Configuration (Internal)")]
    [EndpointDescription("Returns auth provider configuration including secrets. For BFF internal use only. Protected by setup token.")]
    [ProducesResponseType(typeof(AuthProviderConfigurationDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthProviderConfigurationDto>> GetAuthProviderConfigurationInternal(CancellationToken cancellationToken = default)
    {
        var service = HttpContext.RequestServices.GetRequiredService<IAuthProviderConfigurationService>();
        var configuration = await service.ReadConfigurationWithSecretsAsync();
        return Ok(configuration);
    }

    [AllowAnonymous]
    [SetupSecretRequired]
    [EndpointClassification(EndpointClass.Admin)]
    [HttpPut("auth-provider-configuration")]
    [EnableRateLimiting("SetupSecret")]
    [EndpointSummary("Save Auth Provider Configuration (Setup)")]
    [EndpointDescription("Saves auth provider configuration during instance setup. Protected by setup token. At least one provider must be enabled.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> SaveAuthProviderConfiguration([FromBody] AuthProviderConfigurationDto configuration, CancellationToken cancellationToken = default)
    {
        var command = new SaveAuthProviderConfigurationCommand
        {
            Configuration = configuration
        };

        var response = await _mediator.Send(command, cancellationToken);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [AllowAnonymous]
    [SetupSecretRequired]
    [EndpointClassification(EndpointClass.Admin)]
    [HttpGet("authz-provider-configuration/internal")]
    [EndpointSummary("Get Authorization Provider Configuration (Internal)")]
    [EndpointDescription("Returns authorization provider configuration for setup flow. Protected by setup token.")]
    [ProducesResponseType(typeof(AuthorizationProviderConfigurationDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthorizationProviderConfigurationDto>> GetAuthorizationProviderConfigurationInternal(CancellationToken cancellationToken = default)
    {
        var configuration = await _mediator.Send(new GetAuthorizationProviderConfigurationQuery(), cancellationToken);
        return Ok(configuration);
    }

    [AllowAnonymous]
    [SetupSecretRequired]
    [EndpointClassification(EndpointClass.Admin)]
    [HttpPut("authz-provider-configuration")]
    [EnableRateLimiting("SetupSecret")]
    [EndpointSummary("Save Authorization Provider Configuration (Setup)")]
    [EndpointDescription("Saves authorization provider configuration during instance setup. Protected by setup token.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> SaveAuthorizationProviderConfiguration([FromBody] AuthorizationProviderConfigurationDto configuration, CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(
            new SaveAuthorizationProviderConfigurationCommand { Configuration = configuration },
            cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [AllowAnonymous]
    [SetupSecretRequired]
    [EndpointClassification(EndpointClass.Admin)]
    [HttpPost("authz-provider-configuration/verify")]
    [EnableRateLimiting("SetupSecret")]
    [EndpointSummary("Verify Cerbos Authorization Endpoint")]
    [EndpointDescription("Verifies a Cerbos gRPC endpoint by calling its gRPC health service. Protected by setup token.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> VerifyAuthorizationProviderEndpoint(
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] VerifyCerbosEndpointRequest? request,
        CancellationToken cancellationToken = default)
    {
        var command = new VerifyCerbosEndpointCommand
        {
            GrpcEndpoint = request?.GrpcEndpoint ?? string.Empty
        };

        var response = await _mediator.Send(command, cancellationToken);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

}

public class ValidateSetupSecretRequest
{
    public string? Secret { get; set; }
}

public class VerifyCerbosEndpointRequest
{
    public string? GrpcEndpoint { get; set; }
}

public class UpdateDeploymentModeRequest
{
    public string? DeploymentMode { get; set; }
}

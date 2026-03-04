// ABOUTME: API controller for first-run instance onboarding, governance, storage, and auth provider settings.
// ABOUTME: Provides status, completion, governance update, storage settings, and auth provider configuration endpoints.

using System;
using System.Security.Claims;
using Asp.Versioning;
using Explore.API.Filters;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Features.InstanceOnboarding.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
public class InstanceOnboardingController : ControllerBase
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

    [HttpGet("status")]
    [AllowAnonymous]
    [EndpointSummary("Get Instance Onboarding Status")]
    [EndpointDescription("Returns whether first-run onboarding is completed and whether the current user is instance admin.")]
    [ProducesResponseType(typeof(InstanceOnboardingStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstanceOnboardingStatusDto>> GetStatus(CancellationToken cancellationToken = default)
    {
        var status = await _mediator.Send(new GetInstanceOnboardingStatusQuery(), cancellationToken);
        return Ok(status);
    }

    [HttpGet("settings")]
    [Authorize]
    [EndpointSummary("Get Instance Governance Settings")]
    [EndpointDescription("Returns instance governance settings. If onboarding is already complete, only instance admins can access this endpoint.")]
    [ProducesResponseType(typeof(InstanceGovernanceSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<InstanceGovernanceSettingsDto>> GetSettings(CancellationToken cancellationToken = default)
    {
        var status = await _mediator.Send(new GetInstanceOnboardingStatusQuery(), cancellationToken);
        if (status.IsCompleted && !status.IsCurrentUserInstanceAdmin)
        {
            return Forbid();
        }

        var settings = await _mediator.Send(new GetInstanceGovernanceSettingsQuery(), cancellationToken);
        return Ok(settings);
    }

    [HttpPost("complete")]
    [Authorize]
    [SetupSecretRequired]
    [EnableRateLimiting("SetupSecret")]
    [EndpointSummary("Complete Instance Onboarding")]
    [EndpointDescription("Completes first-run onboarding, assigns the current user as instance admin, and persists instance governance settings.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Complete([FromBody] InstanceGovernanceSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
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
            Settings = settings
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

    [HttpPut("settings")]
    [Authorize]
    [EndpointSummary("Update Instance Governance Settings")]
    [EndpointDescription("Updates instance governance settings at runtime. Requires instance administrator membership.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateSettings([FromBody] InstanceGovernanceSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return BadRequest(new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = "Invalid user identity."
            });
        }

        var command = new UpdateInstanceGovernanceSettingsCommand
        {
            UserId = currentUserId.Value,
            Settings = settings
        };

        var response = await _mediator.Send(command, cancellationToken);
        if (!response.Success)
        {
            if (response.Message.Contains("Only instance administrators", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpGet("storage-settings")]
    [Authorize]
    [EndpointSummary("Get Instance Storage Settings")]
    [EndpointDescription("Returns instance S3 storage settings. Only instance admins can access this endpoint.")]
    [ProducesResponseType(typeof(InstanceStorageSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<InstanceStorageSettingsDto>> GetStorageSettings(CancellationToken cancellationToken = default)
    {
        var status = await _mediator.Send(new GetInstanceOnboardingStatusQuery(), cancellationToken);
        if (status.IsCompleted && !status.IsCurrentUserInstanceAdmin)
        {
            return Forbid();
        }

        var settings = await _mediator.Send(new GetInstanceStorageSettingsQuery(), cancellationToken);
        return Ok(settings);
    }

    [HttpPut("storage-settings")]
    [Authorize]
    [EndpointSummary("Update Instance Storage Settings")]
    [EndpointDescription("Updates instance S3 storage settings. Requires instance administrator membership.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateStorageSettings([FromBody] InstanceStorageSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return BadRequest(new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = "Invalid user identity."
            });
        }

        var command = new UpdateInstanceStorageSettingsCommand
        {
            UserId = currentUserId.Value,
            Settings = settings
        };

        var response = await _mediator.Send(command, cancellationToken);
        if (!response.Success)
        {
            if (response.Message.Contains("Only instance administrators", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost("test-storage")]
    [Authorize]
    [EndpointSummary("Test Storage Connection")]
    [EndpointDescription("Tests the S3 storage connection using current settings. Returns success or failure with message.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> TestStorageConnection(CancellationToken cancellationToken = default)
    {
        var status = await _mediator.Send(new GetInstanceOnboardingStatusQuery(), cancellationToken);
        if (status.IsCompleted && !status.IsCurrentUserInstanceAdmin)
        {
            return Forbid();
        }

        var storageService = HttpContext.RequestServices.GetRequiredService<Explore.Application.Contracts.Infrastructure.IObjectStorageService>();
        var success = await storageService.TestConnectionAsync(cancellationToken);

        return Ok(new { success, message = success ? "Connection successful." : "Connection failed. Please verify your S3 settings." });
    }

    [HttpGet("smtp-settings")]
    [Authorize]
    [EndpointSummary("Get Instance SMTP Settings")]
    [EndpointDescription("Returns instance SMTP settings. Only instance admins can access this endpoint.")]
    [ProducesResponseType(typeof(InstanceSmtpSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<InstanceSmtpSettingsDto>> GetSmtpSettings(CancellationToken cancellationToken = default)
    {
        var status = await _mediator.Send(new GetInstanceOnboardingStatusQuery(), cancellationToken);
        if (status.IsCompleted && !status.IsCurrentUserInstanceAdmin)
        {
            return Forbid();
        }

        var settings = await _mediator.Send(new GetInstanceSmtpSettingsQuery(), cancellationToken);
        return Ok(settings);
    }

    [HttpPut("smtp-settings")]
    [Authorize]
    [EndpointSummary("Update Instance SMTP Settings")]
    [EndpointDescription("Updates instance SMTP settings. Requires instance administrator membership.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateSmtpSettings([FromBody] InstanceSmtpSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return BadRequest(new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = "Invalid user identity."
            });
        }

        var command = new UpdateInstanceSmtpSettingsCommand
        {
            UserId = currentUserId.Value,
            Settings = settings
        };

        var response = await _mediator.Send(command, cancellationToken);
        if (!response.Success)
        {
            if (response.Message.Contains("Only instance administrators", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost("test-smtp")]
    [Authorize]
    [EndpointSummary("Test SMTP Connection")]
    [EndpointDescription("Tests the SMTP connection using current settings. Returns success or failure with message.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> TestSmtpConnection(CancellationToken cancellationToken = default)
    {
        var status = await _mediator.Send(new GetInstanceOnboardingStatusQuery(), cancellationToken);
        if (status.IsCompleted && !status.IsCurrentUserInstanceAdmin)
        {
            return Forbid();
        }

        var emailService = HttpContext.RequestServices.GetRequiredService<Explore.Application.Contracts.Infrastructure.IEmailService>();
        var result = await emailService.TestConnectionAsync(cancellationToken);

        var message = result.Success
            ? (string.IsNullOrWhiteSpace(result.Message) ? "Connection successful." : result.Message)
            : (string.IsNullOrWhiteSpace(result.ErrorMessage) ? "Connection failed. Please verify your SMTP settings." : result.ErrorMessage);

        return Ok(new { success = result.Success, message });
    }

    [HttpPost("validate-secret")]
    [AllowAnonymous]
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

    [HttpGet("auth-provider-configuration")]
    [AllowAnonymous]
    [EndpointSummary("Get Auth Provider Configuration")]
    [EndpointDescription("Returns current auth provider configuration. Secrets are redacted. Accessible during setup (anonymous) and by instance admins after onboarding.")]
    [ProducesResponseType(typeof(AuthProviderConfigurationDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthProviderConfigurationDto>> GetAuthProviderConfiguration(CancellationToken cancellationToken = default)
    {
        var status = await _mediator.Send(new GetInstanceOnboardingStatusQuery(), cancellationToken);
        if (status.IsCompleted && !status.IsCurrentUserInstanceAdmin)
        {
            return Forbid();
        }

        var configuration = await _mediator.Send(new GetAuthProviderConfigurationQuery(), cancellationToken);
        return Ok(configuration);
    }

    [HttpGet("auth-provider-configuration/internal")]
    [AllowAnonymous]
    [SetupSecretRequired]
    [EndpointSummary("Get Auth Provider Configuration (Internal)")]
    [EndpointDescription("Returns auth provider configuration including secrets. For BFF internal use only. Protected by setup token.")]
    [ProducesResponseType(typeof(AuthProviderConfigurationDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthProviderConfigurationDto>> GetAuthProviderConfigurationInternal(CancellationToken cancellationToken = default)
    {
        var service = HttpContext.RequestServices.GetRequiredService<IAuthProviderConfigurationService>();
        var configuration = await service.ReadConfigurationWithSecretsAsync();
        return Ok(configuration);
    }

    [HttpPut("auth-provider-configuration")]
    [AllowAnonymous]
    [SetupSecretRequired]
    [EnableRateLimiting("SetupSecret")]
    [EndpointSummary("Save Auth Provider Configuration")]
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

    [HttpPut("admin/auth-provider-configuration")]
    [Authorize]
    [EndpointSummary("Update Auth Provider Configuration")]
    [EndpointDescription("Updates auth provider configuration after onboarding. Requires instance administrator membership and blocks self-lockout.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateAuthProviderConfiguration([FromBody] AuthProviderConfigurationDto configuration, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return BadRequest(new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = "Invalid user identity."
            });
        }

        var command = new UpdateAuthProviderConfigurationCommand
        {
            UserId = currentUserId.Value,
            Configuration = configuration
        };

        var response = await _mediator.Send(command, cancellationToken);
        if (!response.Success)
        {
            if (response.Message.Contains("Only instance administrators", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpGet("auth-provider-configured")]
    [AllowAnonymous]
    [EndpointSummary("Check Auth Provider Configuration Status")]
    [EndpointDescription("Returns whether any auth provider has been configured. Used by the setup flow to determine if the auth provider configuration step should be shown.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> IsAuthProviderConfigured(CancellationToken cancellationToken = default)
    {
        var service = HttpContext.RequestServices.GetRequiredService<IAuthProviderConfigurationService>();
        var isConfigured = await service.IsConfiguredAsync();
        return Ok(new { configured = isConfigured });
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst("internal_user_id")?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sid")?.Value;

        return Guid.TryParse(claim, out var parsedUserId) ? parsedUserId : null;
    }
}

public class ValidateSetupSecretRequest
{
    public string? Secret { get; set; }
}

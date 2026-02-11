// ABOUTME: API controller for first-run instance onboarding, governance, and storage settings.
// ABOUTME: Provides status, completion, governance update, and storage settings endpoints.

using System;
using System.Security.Claims;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Features.InstanceOnboarding.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class InstanceOnboardingController : ControllerBase
{
    private readonly IMediator _mediator;

    public InstanceOnboardingController(IMediator mediator)
    {
        _mediator = mediator;
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

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sid")?.Value;

        return Guid.TryParse(claim, out var parsedUserId) ? parsedUserId : null;
    }
}

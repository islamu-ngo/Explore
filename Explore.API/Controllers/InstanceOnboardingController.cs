// ABOUTME: API controller for first-run instance onboarding and runtime instance governance settings.
// ABOUTME: Provides status, completion, and update endpoints backed by onboarding CQRS handlers.

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

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sid")?.Value;

        return Guid.TryParse(claim, out var parsedUserId) ? parsedUserId : null;
    }
}

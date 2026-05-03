// ABOUTME: API controller for tenant onboarding status and tenant policy onboarding actions.
// ABOUTME: Exposes tenant onboarding questionnaire state and completion/update endpoints.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Filters;
using Explore.API.Hateoas;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.DTOs.TenantPolicy;
using Explore.Application.Features.TenantOnboarding.Requests.Commands;
using Explore.Application.Features.TenantOnboarding.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[EndpointClassification(EndpointClass.Authenticated)]
[RequireMultiTenant]
public class TenantOnboardingController : ExploreControllerBase
{
    private readonly IMediator _mediator;

    public TenantOnboardingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("status", Name = RouteNames.GetTenantOnboardingStatus)]
    [Authorize]
    [EndpointSummary("Get Tenant Onboarding Status")]
    [EndpointDescription("Returns whether the current tenant onboarding has been completed and whether the current user can complete it.")]
    [ProducesResponseType(typeof(TenantOnboardingStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TenantOnboardingStatusDto>> GetStatus(CancellationToken cancellationToken = default)
    {
        var status = await _mediator.Send(new GetTenantOnboardingStatusQuery(), cancellationToken);
        return Ok(status);
    }

    [HttpGet("settings", Name = RouteNames.GetTenantOnboardingPolicySettings)]
    [Authorize]
    [EndpointSummary("Get Tenant Policy Settings")]
    [EndpointDescription("Returns effective tenant policy settings used for tenant onboarding and runtime settings management.")]
    [ProducesResponseType(typeof(TenantPolicySettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TenantPolicySettingsDto>> GetSettings(CancellationToken cancellationToken = default)
    {
        var settings = await _mediator.Send(new GetTenantPolicySettingsQuery(), cancellationToken);
        return Ok(settings);
    }

    [HttpPost("complete", Name = RouteNames.CompleteTenantOnboarding)]
    [Authorize]
    [EndpointSummary("Complete Tenant Onboarding")]
    [EndpointDescription("Completes tenant onboarding and persists tenant policy answers.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Complete([FromBody] UpdateTenantPolicyRequest settings, CancellationToken cancellationToken = default)
    {
        var currentUserId = await ResolveCurrentUserIdAsync(_mediator, cancellationToken);
        if (!currentUserId.HasValue)
        {
            return BadRequest(new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = "Invalid user identity."
            });
        }

        var response = await _mediator.Send(new CompleteTenantOnboardingCommand
        {
            UserId = currentUserId.Value,
            Settings = settings
        }, cancellationToken);

        if (!response.Success)
        {
            if (response.Message?.Contains("Only tenant administrators", StringComparison.OrdinalIgnoreCase) == true)
            {
                return Forbid();
            }

            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPut("settings", Name = RouteNames.UpdateTenantOnboardingPolicySettings)]
    [Authorize]
    [EndpointSummary("Update Tenant Policy Settings")]
    [EndpointDescription("Updates tenant policy settings after onboarding.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateSettings([FromBody] UpdateTenantPolicyRequest settings, CancellationToken cancellationToken = default)
    {
        var currentUserId = await ResolveCurrentUserIdAsync(_mediator, cancellationToken);
        if (!currentUserId.HasValue)
        {
            return BadRequest(new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = "Invalid user identity."
            });
        }

        var response = await _mediator.Send(new UpdateTenantPolicySettingsCommand
        {
            UserId = currentUserId.Value,
            Settings = settings
        }, cancellationToken);

        if (!response.Success)
        {
            if (response.Message?.Contains("Only tenant administrators", StringComparison.OrdinalIgnoreCase) == true)
            {
                return Forbid();
            }

            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPut("steps", Name = RouteNames.SaveTenantOnboardingStepProgress)]
    [Authorize]
    [EndpointSummary("Save Tenant Onboarding Step Progress")]
    [EndpointDescription("Persists tenant onboarding step progress without completing onboarding.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> SaveStep([FromBody] SaveTenantOnboardingStepDto dto, CancellationToken cancellationToken = default)
    {
        var currentUserId = await ResolveCurrentUserIdAsync(_mediator, cancellationToken);
        if (!currentUserId.HasValue)
        {
            return BadRequest(new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = "Invalid user identity."
            });
        }

        var command = new SaveTenantOnboardingStepCommand
        {
            UserId = currentUserId.Value,
            CurrentStep = dto.CurrentStep,
            TotalSteps = dto.TotalSteps,
            CompletedSteps = dto.CompletedSteps
        };

        var response = await _mediator.Send(command, cancellationToken);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    public sealed record SaveTenantOnboardingStepDto(int CurrentStep, int TotalSteps, string[] CompletedSteps);

}

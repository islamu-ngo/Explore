// ABOUTME: Public system metadata endpoints for safe startup and onboarding decisions.
// ABOUTME: Exposes non-sensitive configuration state without requiring Blazor to read API secrets.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Hateoas;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.InstanceOnboarding.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
public sealed class SystemController : ExploreControllerBase
{
    private readonly IMediator _mediator;

    public SystemController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("onboarding-status", Name = RouteNames.GetSystemOnboardingStatus)]
    [OutputCache(PolicyName = "SystemConfig")]
    [EndpointSummary("Get System Onboarding Status")]
    [EndpointDescription("Returns non-sensitive startup state: whether onboarding is required and the effective deployment mode.")]
    [ProducesResponseType(typeof(SystemOnboardingStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemOnboardingStatusDto>> GetOnboardingStatus(CancellationToken cancellationToken = default)
    {
        var status = await _mediator.Send(new GetSystemOnboardingStatusQuery(), cancellationToken);
        return Ok(status);
    }

    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("onboarding-preflight", Name = RouteNames.GetSystemOnboardingPreflight)]
    [EndpointSummary("Get System Onboarding Preflight")]
    [EndpointDescription("Returns non-sensitive blocking checks and operational warnings for first-run launch readiness.")]
    [ProducesResponseType(typeof(OnboardingPreflightDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OnboardingPreflightDto>> GetOnboardingPreflight(CancellationToken cancellationToken = default)
    {
        var preflight = await _mediator.Send(new GetOnboardingPreflightQuery(), cancellationToken);
        return Ok(preflight);
    }
}

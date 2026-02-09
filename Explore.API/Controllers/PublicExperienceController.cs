// ABOUTME: Anonymous-safe API surface for effective tenant public experience settings.
// ABOUTME: Exposes home-page routing and white-label values resolved from cascading policies.

using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.PublicExperience.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class PublicExperienceController : ControllerBase
{
    private readonly IMediator _mediator;

    public PublicExperienceController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("settings")]
    [AllowAnonymous]
    [EndpointSummary("Get Public Experience Settings")]
    [EndpointDescription("Returns effective home-page and white-label settings for the current tenant context.")]
    [ProducesResponseType(typeof(PublicExperienceSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PublicExperienceSettingsDto>> GetSettings(CancellationToken cancellationToken = default)
    {
        var settings = await _mediator.Send(new GetPublicExperienceSettingsQuery(), cancellationToken);
        return Ok(settings);
    }
}

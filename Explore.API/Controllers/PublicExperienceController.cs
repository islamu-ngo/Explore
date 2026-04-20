// ABOUTME: Anonymous-safe API surface for effective tenant public experience settings.
// ABOUTME: Exposes home-page routing and white-label values resolved from cascading policies.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Hateoas;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.PublicExperience.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[EndpointClassification(EndpointClass.Public)]
public class PublicExperienceController : ControllerBase
{
    private readonly IMediator _mediator;

    public PublicExperienceController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("settings", Name = RouteNames.GetPublicExperienceSettings)]
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

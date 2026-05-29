// ABOUTME: Authenticated API surface for AI assistant bootstrap and future conversation workflows.
// ABOUTME: Exposes safe HAL bootstrap metadata while keeping provider secrets and history private.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Hateoas;
using Explore.Application.DTOs.Ai;
using Explore.Application.Features.AiAssistant.Requests.Queries;
using Explore.Application.Hateoas;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/ai/assistant")]
[ApiController]
[Authorize]
[EndpointClassification(EndpointClass.Authenticated)]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public sealed class AiAssistantController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHateoasLinkGenerator _linkGenerator;

    public AiAssistantController(IMediator mediator, IHateoasLinkGenerator linkGenerator)
    {
        _mediator = mediator;
        _linkGenerator = linkGenerator;
    }

    [HttpGet("bootstrap", Name = RouteNames.GetAiAssistantBootstrap)]
    [EndpointSummary("Get AI assistant bootstrap")]
    [EndpointDescription("Returns authenticated AI assistant availability, model choices, feature flags, limits, and HAL links without exposing provider secrets.")]
    [ProducesResponseType(typeof(HalResource<AiAssistantBootstrapDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<HalResource<AiAssistantBootstrapDto>>> GetBootstrap(CancellationToken cancellationToken = default)
    {
        var bootstrap = await _mediator.Send(new GetAiAssistantBootstrapQuery(), cancellationToken);
        var resource = new HalResource<AiAssistantBootstrapDto>(bootstrap);
        var selfPath = _linkGenerator.GeneratePath(RouteNames.GetAiAssistantBootstrap, null, HttpContext);

        if (!string.IsNullOrWhiteSpace(selfPath))
        {
            resource.WithLink(LinkRelations.Self, HalLink.Create(selfPath));
        }

        return Ok(resource);
    }
}

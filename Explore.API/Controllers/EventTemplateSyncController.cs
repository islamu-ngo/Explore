// ABOUTME: REST API controller for event template sync diff/apply/history endpoints.
// ABOUTME: Adds the HTTP boundary over existing Application-layer event template sync workflows.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Extensions;
using Explore.API.Hateoas;
using Explore.API.Models;
using Explore.Application.DTOs.EventTemplateSync;
using Explore.Application.Features.EventTemplateSync.Commands.ApplyEventTemplateSync;
using Explore.Application.Features.EventTemplateSync.Queries.GetEventTemplateDiff;
using Explore.Application.Features.EventTemplateSync.Queries.GetEventTemplateSyncHistory;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/events")]
[ApiController]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
[Authorize(Policy = "template_admin")]
[EndpointClassification(EndpointClass.Authenticated)]
public sealed class EventTemplateSyncController : ExploreControllerBase
{
    private readonly IMediator _mediator;

    public EventTemplateSyncController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Compute the event-template diff for a requested target template version.
    /// </summary>
    [HttpGet("{eventId:guid}/template-sync/diff", Name = RouteNames.GetEventTemplateSyncDiff)]
    [ProducesResponseType(typeof(TemplateDiffDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<TemplateDiffDto>>> GetDiff(
        Guid eventId,
        [FromQuery(Name = "templateVersion")] int templateVersion,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(
            new GetEventTemplateDiffQuery(eventId, templateVersion),
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Apply an operator-selected event-template sync plan.
    /// </summary>
    [HttpPost("{eventId:guid}/template-sync/apply", Name = RouteNames.ApplyEventTemplateSync)]
    [RequestTimeout(RequestTimeoutExtensions.ComplexPolicy)]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(TemplateSyncOutcomeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<TemplateSyncOutcomeDto>>> Apply(
        Guid eventId,
        [FromBody] EventTemplateSyncApplyRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(
            new ApplyEventTemplateSyncCommand(eventId, request.Plan, request.BaseProvenanceVersion),
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Get paged event-template sync audit history.
    /// </summary>
    [HttpGet("{eventId:guid}/template-sync/history", Name = RouteNames.GetEventTemplateSyncHistory)]
    [ProducesResponseType(typeof(PaginatedResult<EventTemplateSyncHistoryItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResult<EventTemplateSyncHistoryItemDto>>> GetHistory(
        Guid eventId,
        [FromQuery(Name = "page")] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(
            new GetEventTemplateSyncHistoryQuery(eventId, page, pageSize),
            cancellationToken);

        return Ok(response);
    }
}

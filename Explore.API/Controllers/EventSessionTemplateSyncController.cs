// ABOUTME: REST API controller for event-session template sync diff/apply/history endpoints.
// ABOUTME: Adds the HTTP boundary over existing Application-layer event-session template sync workflows.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Extensions;
using Explore.API.Hateoas;
using Explore.API.Models;
using Explore.Application.DTOs.EventSessionTemplateSync;
using Explore.Application.Features.EventSessionTemplateSync.Commands.ApplyEventSessionTemplateSync;
using Explore.Application.Features.EventSessionTemplateSync.Queries.GetEventSessionTemplateDiff;
using Explore.Application.Features.EventSessionTemplateSync.Queries.GetEventSessionTemplateSyncHistory;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/event-sessions")]
[ApiController]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
[Authorize(Policy = "template_admin")]
[EndpointClassification(EndpointClass.Authenticated)]
public sealed class EventSessionTemplateSyncController : ExploreControllerBase
{
    private readonly IMediator _mediator;

    public EventSessionTemplateSyncController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Compute the event-session-template diff for a requested target template version.
    /// </summary>
    [HttpGet("{sessionId:guid}/template-sync/diff", Name = RouteNames.GetEventSessionTemplateSyncDiff)]
    [ProducesResponseType(typeof(TemplateDiffDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<TemplateDiffDto>>> GetDiff(
        Guid sessionId,
        [FromQuery(Name = "templateVersion")] int templateVersion,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(
            new GetEventSessionTemplateDiffQuery(sessionId, templateVersion),
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Apply an operator-selected event-session-template sync plan.
    /// </summary>
    [HttpPost("{sessionId:guid}/template-sync/apply", Name = RouteNames.ApplyEventSessionTemplateSync)]
    [RequestTimeout(RequestTimeoutExtensions.ComplexPolicy)]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(TemplateSyncOutcomeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<TemplateSyncOutcomeDto>>> Apply(
        Guid sessionId,
        [FromBody] EventSessionTemplateSyncApplyRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(
            new ApplyEventSessionTemplateSyncCommand(sessionId, request.Plan, request.BaseProvenanceVersion),
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Get paged event-session-template sync audit history.
    /// </summary>
    [HttpGet("{sessionId:guid}/template-sync/history", Name = RouteNames.GetEventSessionTemplateSyncHistory)]
    [ProducesResponseType(typeof(PaginatedResult<EventSessionTemplateSyncHistoryItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResult<EventSessionTemplateSyncHistoryItemDto>>> GetHistory(
        Guid sessionId,
        [FromQuery(Name = "page")] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(
            new GetEventSessionTemplateSyncHistoryQuery(sessionId, page, pageSize),
            cancellationToken);

        return Ok(response);
    }
}

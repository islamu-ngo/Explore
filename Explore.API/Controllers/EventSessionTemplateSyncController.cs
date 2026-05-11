// ABOUTME: REST API controller for event-session template sync diff/apply/history endpoints.
// ABOUTME: Adds the HTTP boundary over existing Application-layer event-session template sync workflows.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Extensions;
using Explore.API.Hateoas;
using Explore.API.Hateoas.Resources;
using Explore.API.Models;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EventSessionTemplateSync;
using Explore.Application.Features.EventSessionTemplateSync.Commands.ApplyEventSessionTemplateSync;
using Explore.Application.Features.EventSessionTemplateSync.Queries.GetEventSessionTemplateDiff;
using Explore.Application.Features.EventSessionTemplateSync.Queries.GetEventSessionTemplateSyncHistory;
using Explore.Application.Hateoas;
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
    private readonly IHateoasLinkGenerator _linkGenerator;
    private readonly ILinkPolicy<EventSessionTemplateSyncResource> _syncLinkPolicy;

    public EventSessionTemplateSyncController(
        IMediator mediator,
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<EventSessionTemplateSyncResource> syncLinkPolicy)
    {
        _mediator = mediator;
        _linkGenerator = linkGenerator;
        _syncLinkPolicy = syncLinkPolicy;
    }

    /// <summary>
    /// Compute the event-session-template diff for a requested target template version.
    /// </summary>
    [HttpGet("{sessionId:guid}/template-sync/diff", Name = RouteNames.GetEventSessionTemplateSyncDiff)]
    [ProducesResponseType(typeof(HalResource<TemplateDiffDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<HalResource<TemplateDiffDto>>> GetDiff(
        Guid sessionId,
        [FromQuery(Name = "templateVersion")] int templateVersion,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(
            new GetEventSessionTemplateDiffQuery(sessionId, templateVersion),
            cancellationToken);

        var diff = response.Id;
        var hasChanges =
            diff.AddedDefinitions.Count > 0 ||
            diff.ModifiedDefinitions.Count > 0 ||
            diff.RetiredDefinitions.Count > 0 ||
            diff.AddedOptions.Count > 0 ||
            diff.ModifiedOptions.Count > 0 ||
            diff.RetiredOptions.Count > 0;

        var resource = new EventSessionTemplateSyncResource(sessionId, diff.TargetTemplateVersion, hasChanges);
        var halResource = new HalResource<TemplateDiffDto>(diff);

        foreach (var linkDef in _syncLinkPolicy.GetLinks(resource, User))
        {
            var halLink = _linkGenerator.GenerateLink(linkDef, HttpContext);
            if (halLink is not null)
            {
                halResource.WithLink(linkDef.Rel, halLink);
            }
        }

        return Ok(halResource);
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
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

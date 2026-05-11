// ABOUTME: API controller for managing language assignments on event sessions.
// ABOUTME: Exposes the existing session-language Application commands for composer language pickers.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Hateoas;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.EventSession;
using Explore.Application.DTOs.EventSessionLanguage;
using Explore.Application.Features.EventSessionLanguages.Requests.Commands;
using Explore.Application.Features.EventSessionLanguages.Requests.Queries;
using Explore.Application.Features.EventSessions.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
public class EventSessionLanguageController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITenantContext _tenantContext;

    public EventSessionLanguageController(IMediator mediator, ITenantContext tenantContext)
    {
        _mediator = mediator;
        _tenantContext = tenantContext;
    }

    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("by-session/{eventSessionId:guid}", Name = RouteNames.GetEventSessionLanguages)]
    [EndpointSummary("Get languages by event session")]
    [EndpointDescription("Get all language assignments for a specific event session.")]
    [ProducesResponseType(typeof(List<EventSessionLanguageListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EventSessionLanguageListDto>>> GetBySession(
        Guid eventSessionId,
        CancellationToken cancellationToken = default)
    {
        var languages = await _mediator.Send(new GetLanguagesBySessionRequest
        {
            EventSessionId = eventSessionId
        }, cancellationToken);

        return Ok(languages);
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost(Name = RouteNames.CreateEventSessionLanguage)]
    [EndpointSummary("Add language to event session")]
    [EndpointDescription("Assign a language to an event session.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<int>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<int>>> Create(
        [FromBody] CreateEventSessionLanguageDto language,
        CancellationToken cancellationToken = default)
    {
        var session = await GetSessionOrNullAsync(language.EventSessionId, cancellationToken);
        if (session is null)
        {
            return NotFound();
        }

        var response = await _mediator.Send(new CreateEventSessionLanguageCommand
        {
            EventSessionLanguageDto = language,
            TenantId = _tenantContext.TenantId,
            EventId = session.EventId
        }, cancellationToken);

        if (!response.Success)
        {
            return this.ToProgramValidationProblem(response, "Event session language creation failed.");
        }

        return CreatedAtRoute(
            RouteNames.GetEventSessionLanguages,
            new { eventSessionId = language.EventSessionId },
            response);
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpDelete("{id:int}", Name = RouteNames.DeleteEventSessionLanguage)]
    [EndpointSummary("Remove language from event session")]
    [EndpointDescription("Remove a language assignment from an event session.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var language = await _mediator.Send(new GetEventSessionLanguageDetailsRequest { Id = id }, cancellationToken);
        if (language is null || language.Id == 0)
        {
            return NotFound();
        }

        var session = await GetSessionOrNullAsync(language.EventSessionId, cancellationToken);
        if (session is null)
        {
            return NotFound();
        }

        var deleted = await _mediator.Send(new DeleteEventSessionLanguageCommand
        {
            Id = id,
            EventSessionId = language.EventSessionId,
            TenantId = _tenantContext.TenantId,
            EventId = session.EventId
        }, cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    private async Task<EventSessionDto?> GetSessionOrNullAsync(Guid eventSessionId, CancellationToken cancellationToken)
    {
        var session = await _mediator.Send(new GetEventSessionDetailsRequest { Id = eventSessionId }, cancellationToken);
        return session is not null && session.Id != Guid.Empty ? session : null;
    }
}

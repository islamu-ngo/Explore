// ABOUTME: API controller for managing language assignments on event sessions.
// ABOUTME: Exposes the existing session-language Application commands for composer language pickers.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
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
    private static readonly ApiNotFoundProblemDescriptor EventSessionNotFoundProblem = new(
        "Event session not found",
        "Event session not found.");

    private static readonly ApiNotFoundProblemDescriptor EventSessionLanguageNotFoundProblem = new(
        "Event session language not found",
        "Event session language not found.");

    private static readonly ApiValidationProblemDescriptor CreateValidationProblem = new(
        "program",
        "Program validation failed",
        "Event session language creation failed.");

    private static readonly ApiValidationProblemDescriptor UpdateValidationProblem = new(
        "program",
        "Program validation failed",
        "Event session language update failed.");

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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<int>>> Create(
        [FromBody] CreateEventSessionLanguageDto language,
        CancellationToken cancellationToken = default)
    {
        var session = await GetSessionOrNullAsync(language.EventSessionId, cancellationToken);
        if (session is null)
        {
            return this.ToNotFoundProblem(EventSessionNotFoundProblem);
        }

        var response = await _mediator.Send(new CreateEventSessionLanguageCommand
        {
            EventSessionLanguageDto = language,
            TenantId = _tenantContext.TenantId,
            EventId = session.EventId
        }, cancellationToken);

        if (!response.Success)
        {
            return this.ToCommandValidationProblem(response, CreateValidationProblem);
        }

        return CreatedAtRoute(
            RouteNames.GetEventSessionLanguages,
            new { eventSessionId = language.EventSessionId },
            response);
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPatch("{id:int}", Name = RouteNames.UpdateEventSessionLanguage)]
    [EndpointSummary("Update event session language assignment")]
    [EndpointDescription("Update the session or language of an event session language assignment.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BaseCommandResponse<int>>> Update(
        int id,
        [FromBody] UpdateEventSessionLanguageDto language,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseConcurrencyStamp(ifMatch, out var expectedConcurrencyStamp))
        {
            ModelState.AddModelError(
                "If-Match",
                "If-Match header is required and must contain the current event session language concurrency stamp.");
            return ValidationProblem(ModelState);
        }

        var existing = await _mediator.Send(new GetEventSessionLanguageDetailsRequest { Id = id }, cancellationToken);
        if (existing is null || existing.Id == 0)
        {
            return this.ToNotFoundProblem(EventSessionLanguageNotFoundProblem);
        }

        var session = await GetSessionOrNullAsync(existing.EventSessionId, cancellationToken);
        if (session is null)
        {
            return this.ToNotFoundProblem(EventSessionNotFoundProblem);
        }

        var response = await _mediator.Send(new UpdateEventSessionLanguageCommand
        {
            EventSessionLanguageId = id,
            EventSessionLanguageDto = language,
            ExpectedConcurrencyStamp = expectedConcurrencyStamp,
            TenantId = _tenantContext.TenantId,
            EventSessionId = existing.EventSessionId,
            EventId = session.EventId
        }, cancellationToken);

        if (!response.Success)
        {
            return this.ToCommandValidationProblem(response, UpdateValidationProblem);
        }

        return Ok(response);
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpDelete("{id:int}", Name = RouteNames.DeleteEventSessionLanguage)]
    [EndpointSummary("Remove language from event session")]
    [EndpointDescription("Remove a language assignment from an event session.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var language = await _mediator.Send(new GetEventSessionLanguageDetailsRequest { Id = id }, cancellationToken);
        if (language is null || language.Id == 0)
        {
            return this.ToNotFoundProblem(EventSessionLanguageNotFoundProblem);
        }

        var session = await GetSessionOrNullAsync(language.EventSessionId, cancellationToken);
        if (session is null)
        {
            return this.ToNotFoundProblem(EventSessionNotFoundProblem);
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
            return this.ToNotFoundProblem(EventSessionLanguageNotFoundProblem);
        }

        return NoContent();
    }

    private async Task<EventSessionDto?> GetSessionOrNullAsync(Guid eventSessionId, CancellationToken cancellationToken)
    {
        var session = await _mediator.Send(new GetEventSessionDetailsRequest { Id = eventSessionId }, cancellationToken);
        return session is not null && session.Id != Guid.Empty ? session : null;
    }

    private static bool TryParseConcurrencyStamp(string? ifMatch, out Guid concurrencyStamp)
    {
        concurrencyStamp = default;

        if (string.IsNullOrWhiteSpace(ifMatch) || ifMatch.StartsWith("W/", StringComparison.Ordinal))
        {
            return false;
        }

        var trimmed = ifMatch.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
        {
            trimmed = trimmed[1..^1];
        }

        return Guid.TryParse(trimmed, out concurrencyStamp) && concurrencyStamp != Guid.Empty;
    }
}

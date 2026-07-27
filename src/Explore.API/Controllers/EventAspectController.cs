// ABOUTME: REST API controller for event Islamic and tech aspect subresources.
// ABOUTME: Keeps aspect-specific routes separate from core event CRUD while preserving route contracts.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Hateoas;
using Explore.API.Models;
using Explore.Application.DTOs.EventAspects;
using Explore.Application.Features.EventAspects.Requests.Commands;
using Explore.Application.Features.EventAspects.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/event")]
[ApiController]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
[Tags("Event")]
public sealed class EventAspectController : ExploreControllerBase
{
    private static readonly ApiValidationProblemDescriptor IslamicAspectValidationProblem = new(
        "eventIslamicAspect",
        "Event Islamic aspect validation failed",
        "Event Islamic aspect update failed.");

    private static readonly ApiValidationProblemDescriptor TechAspectValidationProblem = new(
        "eventTechAspect",
        "Event tech aspect validation failed",
        "Event tech aspect update failed.");

    private static readonly ApiNotFoundProblemDescriptor EventNotFoundProblem = new(
        "Event not found",
        "Event not found.");

    private readonly IMediator _mediator;

    public EventAspectController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get the Islamic aspect for an event.
    /// </summary>
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("{id:guid}/aspects/islamic", Name = RouteNames.GetEventIslamicAspect)]
    [EndpointSummary("Get Event Islamic Aspect")]
    [EndpointDescription("Get the Islamic-specific characteristics of an event (Madhab, prayer timing, gender mode). " +
        "Returns 404 if the event doesn't have an Islamic aspect configured.")]
    [ProducesResponseType(typeof(EventIslamicAspectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<EventIslamicAspectDto>> GetIslamicAspect(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var aspect = await _mediator.Send(new GetEventIslamicAspectRequest { EventId = id }, cancellationToken);

        return Ok(aspect);
    }

    /// <summary>
    /// Create the Islamic aspect for an event.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost("{id:guid}/aspects/islamic", Name = RouteNames.CreateEventIslamicAspect)]
    [EndpointSummary("Create Event Islamic Aspect")]
    [EndpointDescription("Creates the Islamic-specific characteristics of an event. " +
        "Includes Madhab, prayer-based scheduling, gender segregation mode, and language settings.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> CreateIslamicAspect(
        Guid id,
        [FromBody] CreateUpdateIslamicAspectDto aspectDto,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new CreateEventIslamicAspectCommand
        {
            EventId = id,
            AspectDto = aspectDto
        }, cancellationToken);

        if (!response.Success)
        {
            if (response.FailureCode == "event_not_found")
                return this.ToNotFoundProblem(EventNotFoundProblem);

            if (response.FailureCode == "event_islamic_aspect_exists")
                return this.ToCommandConflictProblem(
                    response,
                    "Event Islamic aspect conflict",
                    "Islamic aspect already exists.");

            return this.ToCommandValidationProblem(response, IslamicAspectValidationProblem);
        }

        return CreatedAtRoute(RouteNames.GetEventIslamicAspect, new { id }, response);
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPatch("{id:guid}/aspects/islamic", Name = RouteNames.UpdateEventIslamicAspect)]
    [EndpointSummary("Update Event Islamic Aspect")]
    [EndpointDescription("Partially updates grouped Islamic-specific characteristics for an existing event aspect.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateIslamicAspect(
        Guid id,
        [FromBody] UpdateEventIslamicAspectDto aspectDto,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new UpdateEventIslamicAspectCommand
        {
            EventId = id,
            AspectDto = aspectDto
        }, cancellationToken);

        if (!response.Success)
        {
            if (response.FailureCode is "event_not_found" or "event_islamic_aspect_not_found")
                return this.ToNotFoundProblem(EventNotFoundProblem, response.Message);

            return this.ToCommandValidationProblem(response, IslamicAspectValidationProblem);
        }

        return Ok(response);
    }

    /// <summary>
    /// Delete the Islamic aspect from an event.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpDelete("{id:guid}/aspects/islamic", Name = RouteNames.DeleteEventIslamicAspect)]
    [EndpointSummary("Delete Event Islamic Aspect")]
    [EndpointDescription("Removes the Islamic-specific characteristics from an event. " +
        "The event itself is not deleted.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteIslamicAspect(Guid id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new DeleteEventIslamicAspectCommand { EventId = id }, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Get the Tech aspect for an event.
    /// </summary>
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("{id:guid}/aspects/tech", Name = RouteNames.GetEventTechAspect)]
    [EndpointSummary("Get Event Tech Aspect")]
    [EndpointDescription("Get the tech/developer-specific characteristics of an event (skill level, hackathon details, tech stack). " +
        "Returns 404 if the event doesn't have a Tech aspect configured.")]
    [ProducesResponseType(typeof(EventTechAspectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<EventTechAspectDto>> GetTechAspect(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var aspect = await _mediator.Send(new GetEventTechAspectRequest { EventId = id }, cancellationToken);

        return Ok(aspect);
    }

    /// <summary>
    /// Create the Tech aspect for an event.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost("{id:guid}/aspects/tech", Name = RouteNames.CreateEventTechAspect)]
    [EndpointSummary("Create Event Tech Aspect")]
    [EndpointDescription("Creates the tech/developer-specific characteristics of an event. " +
        "Includes skill level requirements, hackathon track, tech stack tags, and competition details.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> CreateTechAspect(
        Guid id,
        [FromBody] CreateUpdateTechAspectDto aspectDto,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new CreateEventTechAspectCommand
        {
            EventId = id,
            AspectDto = aspectDto
        }, cancellationToken);

        if (!response.Success)
        {
            if (response.FailureCode == "event_not_found")
                return this.ToNotFoundProblem(EventNotFoundProblem);

            if (response.FailureCode == "event_tech_aspect_exists")
                return this.ToCommandConflictProblem(
                    response,
                    "Event Tech aspect conflict",
                    "Tech aspect already exists.");

            return this.ToCommandValidationProblem(response, TechAspectValidationProblem);
        }

        return CreatedAtRoute(RouteNames.GetEventTechAspect, new { id }, response);
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPatch("{id:guid}/aspects/tech", Name = RouteNames.UpdateEventTechAspect)]
    [EndpointSummary("Update Event Tech Aspect")]
    [EndpointDescription("Partially updates grouped tech-specific characteristics for an existing event aspect.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateTechAspect(
        Guid id,
        [FromBody] UpdateEventTechAspectDto aspectDto,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new UpdateEventTechAspectCommand
        {
            EventId = id,
            AspectDto = aspectDto
        }, cancellationToken);

        if (!response.Success)
        {
            if (response.FailureCode is "event_not_found" or "event_tech_aspect_not_found")
                return this.ToNotFoundProblem(EventNotFoundProblem, response.Message);

            return this.ToCommandValidationProblem(response, TechAspectValidationProblem);
        }

        return Ok(response);
    }

    /// <summary>
    /// Delete the Tech aspect from an event.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpDelete("{id:guid}/aspects/tech", Name = RouteNames.DeleteEventTechAspect)]
    [EndpointSummary("Delete Event Tech Aspect")]
    [EndpointDescription("Removes the tech/developer-specific characteristics from an event. " +
        "The event itself is not deleted.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteTechAspect(Guid id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new DeleteEventTechAspectCommand { EventId = id }, cancellationToken);

        return NoContent();
    }
}

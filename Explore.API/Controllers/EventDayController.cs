// ABOUTME: REST API controller for event day CRUD operations scoped to events.
// ABOUTME: Manages event days (multi-day event schedule structure) with HATEOAS.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Hateoas;
using Explore.Application.DTOs.EventDay;
using Explore.Application.Features.EventDays.Requests.Commands;
using Explore.Application.Features.EventDays.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

/// <summary>
/// Event Day management API endpoints.
/// Event days represent individual days within a multi-day event.
/// All responses include HATEOAS links by default.
/// Send "Prefer: return=minimal" header to strip links.
/// </summary>
[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public class EventDayController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<EventDayController> _logger;
    private readonly IResourceAssembler<EventDayDto, EventDayListDto> _resourceAssembler;

    public EventDayController(
        IMediator mediator,
        ILogger<EventDayController> logger,
        IResourceAssembler<EventDayDto, EventDayListDto> resourceAssembler)
    {
        _mediator = mediator;
        _logger = logger;
        _resourceAssembler = resourceAssembler;
    }

    /// <summary>
    /// Get all event days for a specific event.
    /// </summary>
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("by-event/{eventId:guid}", Name = RouteNames.GetEventDaysByEvent)]
    [EndpointSummary("Get Event Days by Event")]
    [EndpointDescription("Get all days for a specific event, ordered by sort order.")]
    [ProducesResponseType(typeof(HalCollectionResource<EventDayListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<HalCollectionResource<EventDayListDto>>> GetByEvent(Guid eventId, CancellationToken cancellationToken = default)
    {
        var days = await _mediator.Send(new GetEventDaysByEventRequest { EventId = eventId }, cancellationToken);

        var halResource = await _resourceAssembler.ToCollectionResource(
            days,
            RouteNames.GetEventDaysByEvent,
            new { eventId },
            HttpContext);

        return Ok(halResource);
    }

    /// <summary>
    /// Get event day details by ID.
    /// </summary>
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("{id:guid}", Name = RouteNames.GetEventDayById)]
    [EndpointSummary("Get Event Day Details")]
    [EndpointDescription("Get detailed information about a specific event day.")]
    [ProducesResponseType(typeof(HalResource<EventDayDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<HalResource<EventDayDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var day = await _mediator.Send(new GetEventDayDetailRequest { Id = id }, cancellationToken);
        if (day == null)
            return NotFound();

        var halResource = await _resourceAssembler.ToResource(day, HttpContext);
        return Ok(halResource);
    }

    /// <summary>
    /// Create a new event day.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost(Name = RouteNames.CreateEventDay)]
    [EndpointSummary("Create Event Day")]
    [EndpointDescription("Create a new event day. Must be associated with an existing event.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventDayDto eventDay, CancellationToken cancellationToken = default)
    {
        var command = new CreateEventDayCommand { EventDayDto = eventDay };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtRoute(
            RouteNames.GetEventDayById,
            new { id = response.Id },
            response);
    }

    /// <summary>
    /// Update an existing event day.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPut("{id:guid}", Name = RouteNames.UpdateEventDay)]
    [EndpointSummary("Update Event Day")]
    [EndpointDescription("Update an existing event day.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateEventDayDto eventDay, CancellationToken cancellationToken = default)
    {
        if (id != eventDay.Id)
        {
            return BadRequest(new { error = "Event day ID mismatch" });
        }

        var command = new UpdateEventDayCommand { EventDayDto = eventDay };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Delete an event day.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpDelete("{id:guid}", Name = RouteNames.DeleteEventDay)]
    [EndpointSummary("Delete Event Day")]
    [EndpointDescription("Delete an event day.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteEventDayCommand { Id = id };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }
}

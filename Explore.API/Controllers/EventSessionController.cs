// ABOUTME: REST API controller for event session CRUD operations with multi-language and speaker support.
// ABOUTME: Manages event sessions, agendas, speakers, and session-level registration with HATEOAS.

using Asp.Versioning;
using Explore.API.Hateoas;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Features.EventSessions.Requests.Commands;
using Explore.Application.Features.EventSessions.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

/// <summary>
/// Event Session management API endpoints.
/// All responses include HATEOAS links by default.
/// Send "Prefer: return=minimal" header to strip links.
/// </summary>
[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public class EventSessionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<EventSessionController> _logger;
    private readonly IResourceAssembler<EventSessionDto, EventSessionListDto> _resourceAssembler;

    public EventSessionController(
        IMediator mediator,
        ILogger<EventSessionController> logger,
        IResourceAssembler<EventSessionDto, EventSessionListDto> resourceAssembler)
    {
        _mediator = mediator;
        _logger = logger;
        _resourceAssembler = resourceAssembler;
    }

    /// <summary>
    /// Get all event sessions with pagination.
    /// </summary>
    [HttpGet(Name = RouteNames.GetEventSessions_List)]
    [EndpointSummary("Get all Event Sessions")]
    [EndpointDescription("Get a paginated list of all event sessions. " +
        "Default page size is 20, max is 100. " +
        "Response includes HATEOAS navigation links. " +
        "Send 'Prefer: return=minimal' header to strip links.")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HalCollectionResource<EventSessionListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<HalCollectionResource<EventSessionListDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetEventSessionListRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);

        var halResource = await _resourceAssembler.ToCollectionResource(
            result,
            RouteNames.GetEventSessions_List,
            additionalRouteValues: null,
            HttpContext);

        return Ok(halResource);
    }

    /// <summary>
    /// Get event session details by ID.
    /// </summary>
    [HttpGet("{id:guid}", Name = RouteNames.GetEventSessionById)]
    [EndpointSummary("Get Event Session Details")]
    [EndpointDescription("Get detailed information about a specific event session. " +
        "Response includes links to related resources (event, speakers, agenda).")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HalResource<EventSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<HalResource<EventSessionDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var session = await _mediator.Send(new GetEventSessionDetailsRequest { Id = id }, cancellationToken);
        if (session == null)
            return NotFound();

        var halResource = await _resourceAssembler.ToResource(session, HttpContext);
        return Ok(halResource);
    }

    /// <summary>
    /// Get sessions for a specific event.
    /// </summary>
    [HttpGet("by-event/{eventId:guid}", Name = RouteNames.GetEventSessions)]
    [EndpointSummary("Get Sessions by Event")]
    [EndpointDescription("Get all sessions for a specific event.")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HalCollectionResource<EventSessionListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<HalCollectionResource<EventSessionListDto>>> GetByEvent(Guid eventId, CancellationToken cancellationToken = default)
    {
        var sessions = await _mediator.Send(new GetSessionsByEventRequest { EventId = eventId }, cancellationToken);

        var halResource = await _resourceAssembler.ToCollectionResource(
            sessions,
            RouteNames.GetEventSessions,
            HttpContext);

        return Ok(halResource);
    }

    /// <summary>
    /// Create a new event session.
    /// </summary>
    [HttpPost(Name = RouteNames.CreateEventSession)]
    [EndpointSummary("Create Event Session")]
    [EndpointDescription("Create a new event session. Must be associated with an existing event.")]
    [Authorize]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventSessionDto session, CancellationToken cancellationToken = default)
    {
        var command = new CreateEventSessionCommand { EventSessionDto = session };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtRoute(
            RouteNames.GetEventSessionById,
            new { id = response.Id },
            response);
    }

    /// <summary>
    /// Update an existing event session.
    /// </summary>
    [HttpPut("{id:guid}", Name = RouteNames.UpdateEventSession)]
    [EndpointSummary("Update Event Session")]
    [EndpointDescription("Update an existing event session.")]
    [Authorize]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateEventSessionDto session, CancellationToken cancellationToken = default)
    {
        if (id != session.Id)
        {
            return BadRequest(new { error = "Event session ID mismatch" });
        }

        var command = new UpdateEventSessionCommand { EventSessionDto = session };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Delete an event session.
    /// </summary>
    [HttpDelete("{id:guid}", Name = RouteNames.DeleteEventSession)]
    [EndpointSummary("Delete Event Session")]
    [EndpointDescription("Delete an event session.")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteEventSessionCommand { Id = id };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }
}

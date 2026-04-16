// ABOUTME: REST API controller for event agenda item CRUD and agenda projection operations.
// ABOUTME: Manages non-session schedule entries (breaks, meals, ceremonies) with HATEOAS.

using Asp.Versioning;
using Explore.API.Hateoas;
using Explore.Application.DTOs.Agenda;
using Explore.Application.DTOs.EventAgendaItem;
using Explore.Application.Features.Agenda.Requests.Queries;
using Explore.Application.Features.EventAgendaItems.Requests.Commands;
using Explore.Application.Features.EventAgendaItems.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

/// <summary>
/// Event Agenda Item management API endpoints.
/// Agenda items represent non-session schedule entries (breaks, meals, prayers, ceremonies).
/// Also provides a merged agenda projection combining sessions and agenda items.
/// All responses include HATEOAS links by default.
/// Send "Prefer: return=minimal" header to strip links.
/// </summary>
[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public class EventAgendaItemController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<EventAgendaItemController> _logger;
    private readonly IResourceAssembler<EventAgendaItemDto, EventAgendaItemListDto> _resourceAssembler;

    public EventAgendaItemController(
        IMediator mediator,
        ILogger<EventAgendaItemController> logger,
        IResourceAssembler<EventAgendaItemDto, EventAgendaItemListDto> resourceAssembler)
    {
        _mediator = mediator;
        _logger = logger;
        _resourceAssembler = resourceAssembler;
    }

    /// <summary>
    /// Get all agenda items for a specific event.
    /// </summary>
    [HttpGet("by-event/{eventId:guid}", Name = RouteNames.GetEventAgendaItemsByEvent)]
    [EndpointSummary("Get Agenda Items by Event")]
    [EndpointDescription("Get all agenda items for a specific event, ordered by sort order.")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HalCollectionResource<EventAgendaItemListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<HalCollectionResource<EventAgendaItemListDto>>> GetByEvent(Guid eventId, CancellationToken cancellationToken = default)
    {
        var items = await _mediator.Send(new GetEventAgendaItemsByEventRequest { EventId = eventId }, cancellationToken);

        var halResource = await _resourceAssembler.ToCollectionResource(
            items,
            RouteNames.GetEventAgendaItemsByEvent,
            HttpContext);

        return Ok(halResource);
    }

    /// <summary>
    /// Get event agenda item details by ID.
    /// </summary>
    [HttpGet("{id:guid}", Name = RouteNames.GetEventAgendaItemById)]
    [EndpointSummary("Get Agenda Item Details")]
    [EndpointDescription("Get detailed information about a specific agenda item.")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HalResource<EventAgendaItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<HalResource<EventAgendaItemDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _mediator.Send(new GetEventAgendaItemDetailRequest { Id = id }, cancellationToken);
        if (item == null)
            return NotFound();

        var halResource = await _resourceAssembler.ToResource(item, HttpContext);
        return Ok(halResource);
    }

    /// <summary>
    /// Get the full agenda projection for an event, merging sessions and agenda items by day and room.
    /// </summary>
    [HttpGet("agenda-projection/{eventId:guid}", Name = RouteNames.GetEventAgendaProjection)]
    [EndpointSummary("Get Event Agenda Projection")]
    [EndpointDescription("Get a merged view of all sessions and agenda items for an event, " +
        "grouped by local day and room with local time projections.")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventAgendaProjectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<EventAgendaProjectionDto>> GetAgendaProjection(Guid eventId, CancellationToken cancellationToken = default)
    {
        var projection = await _mediator.Send(new GetEventAgendaProjectionRequest { EventId = eventId }, cancellationToken);
        if (projection == null)
            return NotFound();

        return Ok(projection);
    }

    /// <summary>
    /// Create a new event agenda item.
    /// </summary>
    [HttpPost(Name = RouteNames.CreateEventAgendaItem)]
    [EndpointSummary("Create Agenda Item")]
    [EndpointDescription("Create a new event agenda item. Must be associated with an existing event.")]
    [Authorize]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventAgendaItemDto agendaItem, CancellationToken cancellationToken = default)
    {
        var command = new CreateEventAgendaItemCommand { EventAgendaItemDto = agendaItem };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtRoute(
            RouteNames.GetEventAgendaItemById,
            new { id = response.Id },
            response);
    }

    /// <summary>
    /// Update an existing event agenda item.
    /// </summary>
    [HttpPut("{id:guid}", Name = RouteNames.UpdateEventAgendaItem)]
    [EndpointSummary("Update Agenda Item")]
    [EndpointDescription("Update an existing event agenda item.")]
    [Authorize]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateEventAgendaItemDto agendaItem, CancellationToken cancellationToken = default)
    {
        if (id != agendaItem.Id)
        {
            return BadRequest(new { error = "Agenda item ID mismatch" });
        }

        var command = new UpdateEventAgendaItemCommand { EventAgendaItemDto = agendaItem };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Delete an event agenda item.
    /// </summary>
    [HttpDelete("{id:guid}", Name = RouteNames.DeleteEventAgendaItem)]
    [EndpointSummary("Delete Agenda Item")]
    [EndpointDescription("Delete an event agenda item.")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteEventAgendaItemCommand { Id = id };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }
}

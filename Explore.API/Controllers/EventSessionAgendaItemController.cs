using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using Explore.Application.DTOs.EventSessionAgendaItem;
using Explore.Application.Features.EventSessionAgendaItems.Requests.Commands;
using Explore.Application.Features.EventSessionAgendaItems.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
public class EventSessionAgendaItemController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<EventSessionAgendaItemController> _logger;

    public EventSessionAgendaItemController(IMediator mediator, ILogger<EventSessionAgendaItemController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // GET: api/v1/eventsessionagendaitem
    [HttpGet]
    [EndpointSummary("Get all Agenda Items")]
    [EndpointDescription("Retrieve a paginated list of all event session agenda items. Default page size is 20, max is 100.")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedResult<EventSessionAgendaItemListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<PaginatedResult<EventSessionAgendaItemListDto>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var agendaItems = await _mediator.Send(new GetEventSessionAgendaItemListRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);
        return Ok(agendaItems);
    }

    // GET: api/v1/eventsessionagendaitem/{id}
    [HttpGet("{id}")]
    [EndpointSummary("Get Agenda Item Details")]
    [EndpointDescription("Get detailed information about a specific agenda item")]
    [AllowAnonymous]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<EventSessionAgendaItemDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var agendaItem = await _mediator.Send(new GetEventSessionAgendaItemDetailsRequest { Id = id }, cancellationToken);

        if (agendaItem == null)
        {
            return NotFound(new { error = "Agenda item not found" });
        }

        return Ok(agendaItem);
    }

    // GET: api/v1/eventsessionagendaitem/by-session/{sessionId}
    [HttpGet("by-session/{sessionId}")]
    [EndpointSummary("Get Agenda Items by Session")]
    [EndpointDescription("Get all agenda items for a specific event session")]
    [AllowAnonymous]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<List<EventSessionAgendaItemListDto>>> GetBySession(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var agendaItems = await _mediator.Send(new GetAgendaItemsBySessionRequest { EventSessionId = sessionId }, cancellationToken);
        return Ok(agendaItems);
    }

    // POST: api/v1/eventsessionagendaitem
    [HttpPost]
    [EndpointSummary("Create Agenda Item")]
    [EndpointDescription("Create a new agenda item for an event session")]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventSessionAgendaItemDto agendaItem, CancellationToken cancellationToken = default)
    {
        var command = new CreateEventSessionAgendaItemCommand { AgendaItemDto = agendaItem };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    // PUT: api/v1/eventsessionagendaitem/{id}
    [HttpPut("{id}")]
    [EndpointSummary("Update Agenda Item")]
    [EndpointDescription("Update an existing agenda item")]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateEventSessionAgendaItemDto agendaItem, CancellationToken cancellationToken = default)
    {
        if (id != agendaItem.Id)
        {
            return BadRequest(new { error = "Agenda item ID mismatch" });
        }

        var command = new UpdateEventSessionAgendaItemCommand { AgendaItemDto = agendaItem };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    // DELETE: api/v1/eventsessionagendaitem/{id}
    [HttpDelete("{id}")]
    [EndpointSummary("Delete Agenda Item")]
    [EndpointDescription("Delete an agenda item")]
    [Authorize]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new DeleteEventSessionAgendaItemCommand { Id = id };
            var result = await _mediator.Send(command, cancellationToken);

            if (!result)
            {
                return NotFound(new { error = "Agenda item not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting agenda item {AgendaItemId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

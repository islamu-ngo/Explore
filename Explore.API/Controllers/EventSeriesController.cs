using Asp.Versioning;
using Explore.API.Hateoas;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.EventSeries.Requests.Commands;
using Explore.Application.Features.EventSeries.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[ApiVersion("1.0")]
[Route("api/[controller]")]
[ApiController]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public class EventSeriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventSeriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<EventSeriesListDto>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] Guid? actorId = null)
    {
        var response = await _mediator.Send(new GetEventSeriesListRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            ActorId = actorId
        });
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventSeriesDto>> GetById(Guid id)
    {
        var response = await _mediator.Send(new GetEventSeriesDetailRequest { Id = id });
        if (response == null)
        {
            return NotFound();
        }
        return Ok(response);
    }

    [HttpGet("top")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventSeriesDto>> GetTop()
    {
        var response = await _mediator.Send(new GetTopEventSeriesRequest());
        if (response == null)
        {
            return NotFound();
        }
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventSeriesDto dto)
    {
        var response = await _mediator.Send(new CreateEventSeriesCommand { EventSeriesDto = dto });
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateEventSeriesDto dto)
    {
        if (id != dto.Id)
        {
            return BadRequest("ID mismatch.");
        }

        var response = await _mediator.Send(new UpdateEventSeriesCommand { EventSeriesDto = dto });
        if (!response.Success)
        {
            return response.Message?.Contains("not found") == true ? NotFound(response) : BadRequest(response);
        }
        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<bool>>> Delete(Guid id)
    {
        var response = await _mediator.Send(new DeleteEventSeriesCommand { Id = id });
        if (!response.Success)
        {
            return NotFound(response);
        }
        return Ok(response);
    }
}

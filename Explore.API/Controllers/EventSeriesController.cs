// ABOUTME: API controller for event series CRUD operations with HATEOAS support.
// ABOUTME: GET endpoints are public, write endpoints require authorization.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Hateoas;
using Explore.API.Models;
using Explore.Application.DTOs.EventSeries;
using Explore.Application.Features.EventSeries.Requests.Commands;
using Explore.Application.Features.EventSeries.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public class EventSeriesController : ControllerBase
{
    private static readonly ApiValidationProblemDescriptor CreateValidationProblem = new(
        "eventSeries",
        "Event series validation failed",
        "Event series creation failed.");

    private static readonly ApiValidationProblemDescriptor UpdateValidationProblem = new(
        "eventSeries",
        "Event series validation failed",
        "Event series update failed.");

    private static readonly ApiNotFoundProblemDescriptor NotFoundProblem = new(
        "Event series not found",
        "The requested event series could not be found.");

    private readonly IMediator _mediator;

    public EventSeriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet(Name = RouteNames.GetEventSeries)]
    [ProducesResponseType(typeof(PaginatedResult<EventSeriesListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResult<EventSeriesListDto>>> GetAll(
        [FromQuery] EventSeriesListQueryRequest query,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new GetEventSeriesListRequest
        {
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            ActorId = query.ActorId
        }, cancellationToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("{id:guid}", Name = RouteNames.GetEventSeriesById)]
    [ProducesResponseType(typeof(EventSeriesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventSeriesDto>> GetById(Guid id)
    {
        var response = await _mediator.Send(new GetEventSeriesDetailRequest { Id = id });

        return Ok(response);
    }

    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("top", Name = RouteNames.GetTopEventSeries)]
    [ProducesResponseType(typeof(EventSeriesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<EventSeriesDto>> GetTop()
    {
        var response = await _mediator.Send(new GetTopEventSeriesRequest());
        if (response == null)
        {
            return NoContent();
        }
        return Ok(response);
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost(Name = RouteNames.CreateEventSeries)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventSeriesDto dto)
    {
        var response = await _mediator.Send(new CreateEventSeriesCommand { EventSeriesDto = dto });
        if (!response.Success)
        {
            return this.ToCommandValidationProblem(response, CreateValidationProblem);
        }
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPut("{id:guid}", Name = RouteNames.UpdateEventSeries)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateEventSeriesDto dto)
    {
        if (id != dto.Id)
        {
            return this.ToValidationProblem(UpdateValidationProblem, "Event series ID mismatch.");
        }

        var response = await _mediator.Send(new UpdateEventSeriesCommand { EventSeriesDto = dto });
        if (!response.Success)
        {
            return response.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true
                ? this.ToNotFoundProblem(NotFoundProblem)
                : this.ToCommandValidationProblem(response, UpdateValidationProblem);
        }
        return Ok(response);
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpDelete("{id:guid}", Name = RouteNames.DeleteEventSeries)]
    [ProducesResponseType(typeof(BaseCommandResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<bool>>> Delete(Guid id)
    {
        var response = await _mediator.Send(new DeleteEventSeriesCommand { Id = id });
        if (!response.Success)
        {
            return this.ToNotFoundProblem(NotFoundProblem);
        }
        return Ok(response);
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using Explore.Application.DTOs.EventStatus;
using Explore.Application.Features.EventStatuses.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
public class EventStatusController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventStatusController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/v1/eventstatus
    [HttpGet]
    [EndpointSummary("Get all Event Statuses")]
    [EndpointDescription("Retrieve a list of all event lifecycle statuses (Draft, Published, Cancelled, Completed)")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<EventStatusListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "LookupData")]
    public async Task<ActionResult<List<EventStatusListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var eventStatuses = await _mediator.Send(new GetEventStatusListRequest(), cancellationToken);
        return Ok(eventStatuses);
    }

    // GET: api/v1/eventstatus/{id}
    [HttpGet("{id}")]
    [EndpointSummary("Get Event Status by ID")]
    [EndpointDescription("Retrieve details of a specific event lifecycle status")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<EventStatusDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var eventStatus = await _mediator.Send(new GetEventStatusDetailsRequest { Id = id }, cancellationToken);
        return Ok(eventStatus);
    }
}

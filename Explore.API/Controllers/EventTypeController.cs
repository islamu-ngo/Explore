// ABOUTME: API controller for event type lookup table (read-only enumeration).
// ABOUTME: Provides available event types for event creation and filtering.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Hateoas;
using Explore.Application.DTOs.EventType;
using Explore.Application.Features.EventTypes.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[EndpointClassification(EndpointClass.Public)]
public class EventTypeController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventTypeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/<EventTypeController>
    [HttpGet(Name = RouteNames.GetEventTypes)]
    [EndpointSummary("Get all Event TYpes")]
    [EndpointDescription("Get A List of all the Event Type Options")]
    [AllowAnonymous]
    [OutputCache(PolicyName = "LookupData")]
    public async Task<ActionResult<List<EventTypeListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var eventTypes = await _mediator.Send(new GetEventTypeListRequest { FullName = string.Empty }, cancellationToken);
        return Ok(eventTypes);
    }

    // GET api/<EventTypeController>/5
    [HttpGet("{id}", Name = RouteNames.GetEventTypeById)]
    [OutputCache(PolicyName = "DetailData")]
    public string Get(int id)
    {
        return "value";
    }

    // POST api/<EventTypeController>
    [HttpPost(Name = RouteNames.CreateEventType)]
    public void Post([FromBody] string value)
    {
    }

    // PUT api/<EventTypeController>/5
    [HttpPut("{id}", Name = RouteNames.UpdateEventType)]
    public void Put(int id, [FromBody] string value)
    {
    }

    // DELETE api/<EventTypeController>/5
    [HttpDelete("{id}", Name = RouteNames.DeleteEventType)]
    public void Delete(int id)
    {
    }
}

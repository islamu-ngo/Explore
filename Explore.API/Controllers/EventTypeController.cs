using Asp.Versioning;
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
public class EventTypeController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EventTypeController(IMediator mediator, IHttpContextAccessor httpContextAccessor)
    {
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
    }

    // GET: api/<EventTypeController>
    [HttpGet]
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
    [HttpGet("{id}")]
    [OutputCache(PolicyName = "DetailData")]
    public string Get(int id)
    {
        return "value";
    }

    // POST api/<EventTypeController>
    [HttpPost]
    public void Post([FromBody] string value)
    {
    }

    // PUT api/<EventTypeController>/5
    [HttpPut("{id}")]
    public void Put(int id, [FromBody] string value)
    {
    }

    // DELETE api/<EventTypeController>/5
    [HttpDelete("{id}")]
    public void Delete(int id)
    {
    }
}

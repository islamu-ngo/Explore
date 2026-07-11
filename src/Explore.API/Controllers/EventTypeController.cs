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

    [HttpGet(Name = RouteNames.GetEventTypes)]
    [EndpointSummary("Get all Event Types")]
    [EndpointDescription("Get A List of all the Event Type Options")]
    [AllowAnonymous]
    [OutputCache(PolicyName = "LookupData")]
    public async Task<ActionResult<List<EventTypeListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var eventTypes = await _mediator.Send(new GetEventTypeListRequest { FullName = string.Empty }, cancellationToken);
        return Ok(eventTypes);
    }
}

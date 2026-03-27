// ABOUTME: API controller for actor type lookup table (read-only enumeration).
// ABOUTME: Provides actor type options (speaker, performer, organizer, etc.) for actor classification.

using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using Explore.Application.DTOs.ActorType;
using Explore.Application.Features.ActorTypes.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
public class ActorTypeController : ControllerBase
{
    private readonly IMediator _mediator;

    public ActorTypeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/actortype
    [HttpGet]
    [EndpointSummary("Get all Actor Types")]
    [EndpointDescription("Retrieve a list of all actor types (User, Organization, Service, Bot)")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<ActorTypeListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "LookupData")]
    public async Task<ActionResult<List<ActorTypeListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var actorTypes = await _mediator.Send(new GetActorTypeListRequest(), cancellationToken);
        return Ok(actorTypes);
    }

    // GET: api/actortype/{id}
    [HttpGet("{id}")]
    [EndpointSummary("Get Actor Type by ID")]
    [EndpointDescription("Retrieve details of a specific actor type")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ActorTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<ActorTypeDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var actorType = await _mediator.Send(new GetActorTypeDetailsRequest { Id = id }, cancellationToken);
        return Ok(actorType);
    }
}

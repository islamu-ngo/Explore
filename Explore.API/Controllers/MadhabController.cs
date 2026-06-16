// ABOUTME: API controller for madhab (Islamic school of thought) lookup table (read-only enumeration).
// ABOUTME: Provides madhab options for event filtering in Islamic module.

using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Hateoas;
using Explore.Application.DTOs.Madhab;
using Explore.Application.Features.Madhabs.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[EndpointClassification(EndpointClass.Public)]
public class MadhabController : ControllerBase
{
    private readonly IMediator _mediator;

    public MadhabController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/madhab
    [HttpGet(Name = RouteNames.GetMadhabs)]
    [EndpointSummary("Get all Madhabs")]
    [EndpointDescription("Retrieve a list of all Islamic jurisprudence schools (Hanafi, Maliki, Shafi'i, Hanbali)")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<MadhabListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "LookupData")]
    public async Task<ActionResult<List<MadhabListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var madhabs = await _mediator.Send(new GetMadhabListRequest(), cancellationToken);
        return Ok(madhabs);
    }

    // GET: api/madhab/{id}
    [HttpGet("{id}", Name = RouteNames.GetMadhabById)]
    [EndpointSummary("Get Madhab by ID")]
    [EndpointDescription("Retrieve details of a specific Islamic jurisprudence school")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MadhabDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<MadhabDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var madhab = await _mediator.Send(new GetMadhabDetailsRequest { Id = id }, cancellationToken);
        return Ok(madhab);
    }
}

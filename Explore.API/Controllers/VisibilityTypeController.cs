using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using Explore.Application.DTOs.VisibilityType;
using Explore.Application.Features.VisibilityTypes.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
public class VisibilityTypeController : ControllerBase
{
    private readonly IMediator _mediator;

    public VisibilityTypeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/v1/visibilitytype
    [HttpGet]
    [EndpointSummary("Get all Visibility Types")]
    [EndpointDescription("Retrieve a list of all event visibility types (Public, Private, Unlisted)")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<VisibilityTypeListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "LookupData")]
    public async Task<ActionResult<List<VisibilityTypeListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var visibilityTypes = await _mediator.Send(new GetVisibilityTypeListRequest(), cancellationToken);
        return Ok(visibilityTypes);
    }

    // GET: api/v1/visibilitytype/{id}
    [HttpGet("{id}")]
    [EndpointSummary("Get Visibility Type by ID")]
    [EndpointDescription("Retrieve details of a specific event visibility type")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(VisibilityTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<VisibilityTypeDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var visibilityType = await _mediator.Send(new GetVisibilityTypeDetailsRequest { Id = id }, cancellationToken);
        return Ok(visibilityType);
    }
}

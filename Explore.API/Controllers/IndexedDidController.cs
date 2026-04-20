// ABOUTME: REST API controller for indexed DID (Decentralized Identifier) CRUD operations.
// ABOUTME: Manages ATProto DID records and federation identity mappings with HATEOAS support.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Hateoas;
using Explore.Application.DTOs.IndexedDid;
using Explore.Application.Features.IndexedDids.Requests.Commands;
using Explore.Application.Features.IndexedDids.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/indexeddid")]
[ApiController]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public class IndexedDidController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<IndexedDidController> _logger;
    private readonly IResourceAssembler<IndexedDidDto, IndexedDidListDto> _resourceAssembler;

    public IndexedDidController(
        IMediator mediator,
        ILogger<IndexedDidController> logger,
        IResourceAssembler<IndexedDidDto, IndexedDidListDto> resourceAssembler)
    {
        _mediator = mediator;
        _logger = logger;
        _resourceAssembler = resourceAssembler;
    }

    // GET: api/indexeddid
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet(Name = RouteNames.GetIndexedDids)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<HalCollectionResource<IndexedDidListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var indexedDids = await _mediator.Send(new GetIndexedDidListRequest(), cancellationToken);
        var halResource = await _resourceAssembler.ToCollectionResource(
            indexedDids,
            RouteNames.GetIndexedDids,
            HttpContext);
        return Ok(halResource);
    }

    // GET: api/indexeddid/{did}
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("{did}", Name = RouteNames.GetIndexedDidByDid)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<HalResource<IndexedDidDto>>> GetById(string did, CancellationToken cancellationToken = default)
    {
        var indexedDid = await _mediator.Send(new GetIndexedDidDetailsRequest { Did = did }, cancellationToken);
        if (indexedDid == null)
            return NotFound();

        var halResource = await _resourceAssembler.ToResource(indexedDid, HttpContext);
        return Ok(halResource);
    }

    // POST: api/indexeddid
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost(Name = RouteNames.CreateIndexedDid)]
    public async Task<ActionResult<BaseCommandResponse<string>>> Create([FromBody] CreateIndexedDidDto dto, CancellationToken cancellationToken = default)
    {
        var command = new CreateIndexedDidCommand { IndexedDidDto = dto };
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    // PUT: api/indexeddid/{did}
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPut("{did}", Name = RouteNames.UpdateIndexedDid)]
    public async Task<ActionResult<BaseCommandResponse<string>>> Update(string did, [FromBody] UpdateIndexedDidDto dto, CancellationToken cancellationToken = default)
    {
        if (did != dto.Did)
        {
            return BadRequest(new { error = "IndexedDid DID mismatch" });
        }

        var command = new UpdateIndexedDidCommand { IndexedDidDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    // DELETE: api/indexeddid/{did}
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpDelete("{did}", Name = RouteNames.DeleteIndexedDid)]
    public async Task<ActionResult> Delete(string did, CancellationToken cancellationToken = default)
    {
        var command = new DeleteIndexedDidCommand { Did = did };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }
}

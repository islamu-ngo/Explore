// ABOUTME: Public read-only API for discovering globally indexed AT Protocol records.
// ABOUTME: Returns public AT identity metadata as HAL without exposing mutation authority.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Hateoas;
using Explore.Application.DTOs.AtprotoRecord;
using Explore.Application.Features.AtprotoRecords.Requests.Queries;
using Explore.Application.Hateoas;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[EndpointClassification(EndpointClass.Public)]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public class AtprotoRecordController : ControllerBase
{
    private static readonly ApiNotFoundProblemDescriptor NotFoundProblem = new(
        "AT Protocol record not found",
        "AT Protocol record not found.");

    private readonly IMediator _mediator;
    private readonly IResourceAssembler<AtprotoRecordDto, AtprotoRecordListDto> _resourceAssembler;

    public AtprotoRecordController(
        IMediator mediator,
        IResourceAssembler<AtprotoRecordDto, AtprotoRecordListDto> resourceAssembler)
    {
        _mediator = mediator;
        _resourceAssembler = resourceAssembler;
    }

    // GET: api/atprotoRecord
    [HttpGet(Name = RouteNames.GetAtprotoRecordEntries)]
    [AllowAnonymous]
    [OutputCache(PolicyName = "ListData")]
    [ProducesResponseType(typeof(HalCollectionResource<AtprotoRecordListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<HalCollectionResource<AtprotoRecordListDto>>> GetAll(
        CancellationToken cancellationToken = default)
    {
        var atprotoRecords = await _mediator.Send(new GetAtprotoRecordListRequest(), cancellationToken);
        var resource = await _resourceAssembler.ToCollectionResource(
            atprotoRecords,
            RouteNames.GetAtprotoRecordEntries,
            HttpContext);
        return Ok(resource);
    }

    // GET: api/atprotoRecord/{id}
    [HttpGet("{id}", Name = RouteNames.GetAtprotoRecordEntryById)]
    [AllowAnonymous]
    [OutputCache(PolicyName = "DetailData")]
    [ProducesResponseType(typeof(HalResource<AtprotoRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HalResource<AtprotoRecordDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var atprotoRecord = await _mediator.Send(new GetAtprotoRecordDetailsRequest { Id = id }, cancellationToken);
        if (atprotoRecord is null)
        {
            return this.ToNotFoundProblem(NotFoundProblem);
        }

        var resource = await _resourceAssembler.ToResource(atprotoRecord, HttpContext);
        return Ok(resource);
    }
}

// ABOUTME: API controller for tag type lookup table (read-only enumeration).
// ABOUTME: Provides tag type categories for event tag classification and filtering.

using Asp.Versioning;
using Explore.Application.DTOs.TagType;
using Explore.Application.Features.TagTypes.Requests.Queries;
using Explore.Application.Features.TagTypeTags.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
public class TagTypeController : ControllerBase
{
    private readonly IMediator _mediator;

    public TagTypeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/tagtype
    [HttpGet]
    [AllowAnonymous]
    [OutputCache(PolicyName = "LookupData")]
    public async Task<ActionResult<List<TagTypeListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var tagTypes = await _mediator.Send(new GetTagTypeListRequest(), cancellationToken);
        return Ok(tagTypes);
    }

    // GET: api/tagtype/{id}
    [HttpGet("{id}")]
    [AllowAnonymous]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<TagTypeDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var tagType = await _mediator.Send(new GetTagTypeDetailsRequest { Id = id }, cancellationToken);
        return Ok(tagType);
    }

    // GET: api/tagtype/with-tags
    [HttpGet("with-tags")]
    [EndpointSummary("Get Tag Types with Tags")]
    [EndpointDescription("Returns all tag types with their associated tags grouped. Used by the tri-state tag filter dropdown.")]
    [AllowAnonymous]
    [OutputCache(PolicyName = "LookupData")]
    [ProducesResponseType(typeof(List<TagTypeWithTagsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TagTypeWithTagsDto>>> GetWithTags(CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetTagsGroupedByTagTypeRequest(), cancellationToken);
        return Ok(result);
    }
}

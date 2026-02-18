using Asp.Versioning;
using Explore.Application.DTOs.TagType;
using Explore.Application.Features.TagTypes.Requests.Queries;
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

    // GET: api/v1/tagtype
    [HttpGet]
    [AllowAnonymous]
    [OutputCache(PolicyName = "LookupData")]
    public async Task<ActionResult<List<TagTypeListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var tagTypes = await _mediator.Send(new GetTagTypeListRequest(), cancellationToken);
        return Ok(tagTypes);
    }

    // GET: api/v1/tagtype/{id}
    [HttpGet("{id}")]
    [AllowAnonymous]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<TagTypeDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var tagType = await _mediator.Send(new GetTagTypeDetailsRequest { Id = id }, cancellationToken);
        return Ok(tagType);
    }
}

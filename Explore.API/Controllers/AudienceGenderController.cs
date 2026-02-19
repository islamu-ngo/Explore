using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using Explore.Application.DTOs.AudienceGender;
using Explore.Application.Features.AudienceGenders.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
public class AudienceGenderController : ControllerBase
{
    private readonly IMediator _mediator;

    public AudienceGenderController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/audiencegender
    [HttpGet]
    [EndpointSummary("Get all Audience Gender types")]
    [EndpointDescription("Retrieve a list of all audience gender types (Men-only, Women-only, Mixed, Family)")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<AudienceGenderListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "LookupData")]
    public async Task<ActionResult<List<AudienceGenderListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var audienceGenders = await _mediator.Send(new GetAudienceGenderListRequest(), cancellationToken);
        return Ok(audienceGenders);
    }

    // GET: api/audiencegender/{id}
    [HttpGet("{id}")]
    [EndpointSummary("Get Audience Gender type by ID")]
    [EndpointDescription("Retrieve details of a specific audience gender type")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AudienceGenderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<AudienceGenderDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var audienceGender = await _mediator.Send(new GetAudienceGenderDetailsRequest { Id = id }, cancellationToken);
        if (audienceGender == null)
        {
            return NotFound();
        }

        return Ok(audienceGender);
    }
}

using Asp.Versioning;
using Explore.Application.DTOs.Language;
using Explore.Application.Features.Languages.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
public class LanguageController : ControllerBase
{
    private readonly IMediator _mediator;

    public LanguageController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/language
    [HttpGet]
    [EndpointSummary("Get all Languages")]
    [EndpointDescription("Get a list of all available languages (lookup table)")]
    [AllowAnonymous]
    [OutputCache(PolicyName = "LookupData")]
    public async Task<ActionResult<List<LanguageListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var languages = await _mediator.Send(new GetLanguageListRequest(), cancellationToken);
        return Ok(languages);
    }

    // GET: api/language/{id}
    [HttpGet("{id}")]
    [EndpointSummary("Get Language Details")]
    [EndpointDescription("Get detailed information about a specific language")]
    [AllowAnonymous]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<LanguageDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var language = await _mediator.Send(new GetLanguageDetailsRequest { Id = id }, cancellationToken);

        if (language == null)
        {
            return NotFound(new { error = "Language not found" });
        }

        return Ok(language);
    }
}

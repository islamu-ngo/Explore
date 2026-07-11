// ABOUTME: API controller for language lookup and discovery endpoints.
// ABOUTME: Provides read-only access to supported languages for event sessions and localization.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Hateoas;
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
[EndpointClassification(EndpointClass.Public)]
public class LanguageController : ControllerBase
{
    private readonly IMediator _mediator;

    public LanguageController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/language
    [HttpGet(Name = RouteNames.GetLanguages)]
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
    [HttpGet("{id}", Name = RouteNames.GetLanguageById)]
    [EndpointSummary("Get Language Details")]
    [EndpointDescription("Get detailed information about a specific language")]
    [AllowAnonymous]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<LanguageDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var language = await _mediator.Send(new GetLanguageDetailsRequest { Id = id }, cancellationToken);

        return Ok(language);
    }
}

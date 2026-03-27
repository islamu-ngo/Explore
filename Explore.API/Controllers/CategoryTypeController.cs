// ABOUTME: API controller for category type lookup table (read-only enumeration).
// ABOUTME: Provides category type groupings for event category organization and filtering.

using Asp.Versioning;
using Explore.Application.DTOs.CategoryType;
using Explore.Application.Features.CategoryTypeCategories.Requests.Queries;
using Explore.Application.Features.CategoryTypes.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
public class CategoryTypeController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoryTypeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/categorytype
    [HttpGet]
    [AllowAnonymous]
    [OutputCache(PolicyName = "LookupData")]
    public async Task<ActionResult<List<CategoryTypeListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var categoryTypes = await _mediator.Send(new GetCategoryTypeListRequest(), cancellationToken);
        return Ok(categoryTypes);
    }

    // GET: api/categorytype/{id}
    [HttpGet("{id}")]
    [AllowAnonymous]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<CategoryTypeDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var categoryType = await _mediator.Send(new GetCategoryTypeDetailsRequest { Id = id }, cancellationToken);
        return Ok(categoryType);
    }

    // GET: api/categorytype/with-categories
    [HttpGet("with-categories")]
    [EndpointSummary("Get Category Types with Categories")]
    [EndpointDescription("Returns all category types with their associated categories grouped. Used by the tri-state category filter dropdown.")]
    [AllowAnonymous]
    [OutputCache(PolicyName = "LookupData")]
    [ProducesResponseType(typeof(List<CategoryTypeWithCategoriesDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CategoryTypeWithCategoriesDto>>> GetWithCategories(CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetCategoriesGroupedByCategoryTypeRequest(), cancellationToken);
        return Ok(result);
    }
}

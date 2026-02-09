using Explore.API.Hateoas;
using Explore.Application.DTOs.Category;
using Explore.Application.Features.Categories.Requests.Commands;
using Explore.Application.Features.Categories.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

/// <summary>
/// Category management API endpoints.
/// All responses include HATEOAS links by default.
/// Send "Prefer: return=minimal" header to strip links.
/// </summary>
[Route("api/v1/[controller]")]
[ApiController]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public class CategoryController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CategoryController> _logger;
    private readonly IResourceAssembler<CategoryDto, CategoryListDto> _resourceAssembler;

    public CategoryController(
        IMediator mediator,
        ILogger<CategoryController> logger,
        IResourceAssembler<CategoryDto, CategoryListDto> resourceAssembler)
    {
        _mediator = mediator;
        _logger = logger;
        _resourceAssembler = resourceAssembler;
    }

    /// <summary>
    /// Get all categories with pagination.
    /// </summary>
    [HttpGet(Name = RouteNames.GetCategories)]
    [EndpointSummary("Get all Categories")]
    [EndpointDescription("Retrieve a paginated list of all event categories. " +
        "Default page size is 20, max is 100. " +
        "Response includes HATEOAS navigation links. " +
        "Send 'Prefer: return=minimal' header to strip links.")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HalCollectionResource<CategoryListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<HalCollectionResource<CategoryListDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetCategoryListRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);

        var halResource = _resourceAssembler.ToCollectionResource(
            result,
            RouteNames.GetCategories,
            additionalRouteValues: null,
            HttpContext);

        return Ok(halResource);
    }

    /// <summary>
    /// Get category details by ID.
    /// </summary>
    [HttpGet("{id:guid}", Name = RouteNames.GetCategoryById)]
    [EndpointSummary("Get Category Details")]
    [EndpointDescription("Get detailed information about a specific category. " +
        "Response includes links to parent category, children, and events.")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HalResource<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<HalResource<CategoryDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _mediator.Send(new GetCategoryDetailsRequest { Id = id }, cancellationToken);

        if (category is null)
        {
            return NotFound(new { error = "Category not found" });
        }

        var halResource = _resourceAssembler.ToResource(category, HttpContext);
        return Ok(halResource);
    }

    /// <summary>
    /// Create a new category.
    /// </summary>
    [HttpPost(Name = RouteNames.CreateCategory)]
    [EndpointSummary("Create Category")]
    [EndpointDescription("Create a new event category. Categories can be nested with parent_id.")]
    [Authorize]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateCategoryDto category, CancellationToken cancellationToken = default)
    {
        var command = new CreateCategoryCommand { CategoryDto = category };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtRoute(
            RouteNames.GetCategoryById,
            new { id = response.Id },
            response);
    }

    /// <summary>
    /// Update an existing category.
    /// </summary>
    [HttpPut("{id:guid}", Name = RouteNames.UpdateCategory)]
    [EndpointSummary("Update Category")]
    [EndpointDescription("Update an existing category's information.")]
    [Authorize]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateCategoryDto category, CancellationToken cancellationToken = default)
    {
        if (id != category.Id)
        {
            return BadRequest(new { error = "Category ID mismatch" });
        }

        var command = new UpdateCategoryCommand { CategoryDto = category };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Delete a category.
    /// </summary>
    [HttpDelete("{id:guid}", Name = RouteNames.DeleteCategory)]
    [EndpointSummary("Delete Category")]
    [EndpointDescription("Delete a category. Will fail if category has child categories.")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new DeleteCategoryCommand { Id = id };
            var result = await _mediator.Send(command, cancellationToken);

            if (!result)
            {
                return NotFound(new { error = "Category not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {CategoryId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

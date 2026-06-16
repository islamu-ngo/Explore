// ABOUTME: REST API controller for event category CRUD operations with HATEOAS support.
// ABOUTME: Manages event categories used for discovery, filtering, and event classification.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Hateoas;
using Explore.API.Models;
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
[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public class CategoryController : ControllerBase
{
    private static readonly ApiValidationProblemDescriptor CreateValidationProblem = new(
        "category",
        "Category validation failed",
        "Category creation failed.");

    private static readonly ApiValidationProblemDescriptor UpdateValidationProblem = new(
        "category",
        "Category validation failed",
        "Category update failed.");

    private static readonly ApiNotFoundProblemDescriptor CategoryNotFoundProblem = new(
        "Category not found",
        "Category not found.");

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
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet(Name = RouteNames.GetCategories)]
    [EndpointSummary("Get all Categories")]
    [EndpointDescription("Retrieve a paginated list of all event categories. " +
        "Default page size is 20, max is 100. " +
        "Response includes HATEOAS navigation links. " +
        "Send 'Prefer: return=minimal' header to strip links.")]
    [ProducesResponseType(typeof(HalCollectionResource<CategoryListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<HalCollectionResource<CategoryListDto>>> GetAll(
        [FromQuery] PaginationQueryRequest query,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetCategoryListRequest
        {
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        }, cancellationToken);

        var halResource = await _resourceAssembler.ToCollectionResource(
            result,
            RouteNames.GetCategories,
            additionalRouteValues: null,
            HttpContext);

        return Ok(halResource);
    }

    /// <summary>
    /// Get category details by ID.
    /// </summary>
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("{id:guid}", Name = RouteNames.GetCategoryById)]
    [EndpointSummary("Get Category Details")]
    [EndpointDescription("Get detailed information about a specific category. " +
        "Response includes links to parent category, children, and events.")]
    [ProducesResponseType(typeof(HalResource<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<HalResource<CategoryDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _mediator.Send(new GetCategoryDetailsRequest { Id = id }, cancellationToken);
        if (category == null)
        {
            return this.ToNotFoundProblem(CategoryNotFoundProblem);
        }

        var halResource = await _resourceAssembler.ToResource(category, HttpContext);
        return Ok(halResource);
    }

    /// <summary>
    /// Create a new category.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost(Name = RouteNames.CreateCategory)]
    [EndpointSummary("Create Category")]
    [EndpointDescription("Create a new event category. Categories can be nested with parent_id.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateCategoryDto category, CancellationToken cancellationToken = default)
    {
        var command = new CreateCategoryCommand { CategoryDto = category };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return this.ToCommandValidationProblem(response, CreateValidationProblem);
        }

        return CreatedAtRoute(
            RouteNames.GetCategoryById,
            new { id = response.Id },
            response);
    }

    /// <summary>
    /// Update an existing category.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPut("{id:guid}", Name = RouteNames.UpdateCategory)]
    [EndpointSummary("Update Category")]
    [EndpointDescription("Update an existing category's information.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateCategoryDto category, CancellationToken cancellationToken = default)
    {
        if (id != category.Id)
        {
            return this.ToValidationProblem(UpdateValidationProblem, "Category ID mismatch.");
        }

        var command = new UpdateCategoryCommand { CategoryDto = category };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return this.ToCommandValidationProblem(response, UpdateValidationProblem);
        }

        return Ok(response);
    }

    /// <summary>
    /// Delete a category.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpDelete("{id:guid}", Name = RouteNames.DeleteCategory)]
    [EndpointSummary("Delete Category")]
    [EndpointDescription("Delete a category. Will fail if category has child categories.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteCategoryCommand { Id = id };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }
}

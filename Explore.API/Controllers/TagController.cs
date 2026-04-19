// ABOUTME: REST API controller for event tag CRUD operations with HATEOAS support.
// ABOUTME: Manages event tags used for categorization and discovery filtering.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Hateoas;
using Explore.Application.DTOs.Tag;
using Explore.Application.Features.Tags.Requests.Commands;
using Explore.Application.Features.Tags.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

/// <summary>
/// Tag management API endpoints.
/// All responses include HATEOAS links by default.
/// Send "Prefer: return=minimal" header to strip links.
/// </summary>
[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public class TagController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TagController> _logger;
    private readonly IResourceAssembler<TagDto, TagListDto> _resourceAssembler;

    public TagController(
        IMediator mediator,
        ILogger<TagController> logger,
        IResourceAssembler<TagDto, TagListDto> resourceAssembler)
    {
        _mediator = mediator;
        _logger = logger;
        _resourceAssembler = resourceAssembler;
    }

    /// <summary>
    /// Get all tags with pagination.
    /// </summary>
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet(Name = RouteNames.GetTags)]
    [EndpointSummary("Get all Tags")]
    [EndpointDescription("Retrieve a paginated list of all tags for events. " +
        "Default page size is 20, max is 100. " +
        "Response includes HATEOAS navigation links. " +
        "Send 'Prefer: return=minimal' header to strip links.")]
    [ProducesResponseType(typeof(HalCollectionResource<TagListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<HalCollectionResource<TagListDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetTagListRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);

        var halResource = await _resourceAssembler.ToCollectionResource(
            result,
            RouteNames.GetTags,
            additionalRouteValues: null,
            HttpContext);

        return Ok(halResource);
    }

    /// <summary>
    /// Get tag details by ID.
    /// </summary>
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("{id:guid}", Name = RouteNames.GetTagById)]
    [EndpointSummary("Get Tag Details")]
    [EndpointDescription("Get detailed information about a specific tag. " +
        "Response includes links to events with this tag.")]
    [ProducesResponseType(typeof(HalResource<TagDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<HalResource<TagDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var tag = await _mediator.Send(new GetTagDetailsRequest { Id = id }, cancellationToken);
        if (tag == null)
            return NotFound();

        var halResource = await _resourceAssembler.ToResource(tag, HttpContext);
        return Ok(halResource);
    }

    /// <summary>
    /// Create a new tag.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost(Name = RouteNames.CreateTag)]
    [EndpointSummary("Create Tag")]
    [EndpointDescription("Create a new tag for categorizing events.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateTagDto tag, CancellationToken cancellationToken = default)
    {
        var command = new CreateTagCommand { TagDto = tag };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtRoute(
            RouteNames.GetTagById,
            new { id = response.Id },
            response);
    }

    /// <summary>
    /// Update an existing tag.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPut("{id:guid}", Name = RouteNames.UpdateTag)]
    [EndpointSummary("Update Tag")]
    [EndpointDescription("Update an existing tag's information.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateTagDto tag, CancellationToken cancellationToken = default)
    {
        if (id != tag.Id)
        {
            return BadRequest(new { error = "Tag ID mismatch" });
        }

        var command = new UpdateTagCommand { TagDto = tag };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Delete a tag.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpDelete("{id:guid}", Name = RouteNames.DeleteTag)]
    [EndpointSummary("Delete Tag")]
    [EndpointDescription("Delete a tag. Events using this tag will be unlinked.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteTagCommand { Id = id };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }
}

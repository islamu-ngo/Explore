using Explore.API.Hateoas;
using Explore.Application.DTOs.Tag;
using Explore.Application.Features.Tags.Requests.Commands;
using Explore.Application.Features.Tags.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

/// <summary>
/// Tag management API endpoints.
/// All responses include HATEOAS links by default.
/// Send "Prefer: return=minimal" header to strip links.
/// </summary>
[Route("api/v1/[controller]")]
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
    [HttpGet(Name = RouteNames.GetTags)]
    [EndpointSummary("Get all Tags")]
    [EndpointDescription("Retrieve a paginated list of all tags for events. " +
        "Default page size is 20, max is 100. " +
        "Response includes HATEOAS navigation links. " +
        "Send 'Prefer: return=minimal' header to strip links.")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HalCollectionResource<TagListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<HalCollectionResource<TagListDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetTagListRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        });

        var halResource = _resourceAssembler.ToCollectionResource(
            result,
            RouteNames.GetTags,
            additionalRouteValues: null,
            HttpContext);

        return Ok(halResource);
    }

    /// <summary>
    /// Get tag details by ID.
    /// </summary>
    [HttpGet("{id:guid}", Name = RouteNames.GetTagById)]
    [EndpointSummary("Get Tag Details")]
    [EndpointDescription("Get detailed information about a specific tag. " +
        "Response includes links to events with this tag.")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HalResource<TagDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HalResource<TagDto>>> GetById(Guid id)
    {
        var tag = await _mediator.Send(new GetTagDetailsRequest { Id = id });

        if (tag is null)
        {
            return NotFound(new { error = "Tag not found" });
        }

        var halResource = _resourceAssembler.ToResource(tag, HttpContext);
        return Ok(halResource);
    }

    /// <summary>
    /// Create a new tag.
    /// </summary>
    [HttpPost(Name = RouteNames.CreateTag)]
    [EndpointSummary("Create Tag")]
    [EndpointDescription("Create a new tag for categorizing events.")]
    [Authorize]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateTagDto tag)
    {
        var command = new CreateTagCommand { TagDto = tag };
        var response = await _mediator.Send(command);

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
    [HttpPut("{id:guid}", Name = RouteNames.UpdateTag)]
    [EndpointSummary("Update Tag")]
    [EndpointDescription("Update an existing tag's information.")]
    [Authorize]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateTagDto tag)
    {
        if (id != tag.Id)
        {
            return BadRequest(new { error = "Tag ID mismatch" });
        }

        var command = new UpdateTagCommand { TagDto = tag };
        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Delete a tag.
    /// </summary>
    [HttpDelete("{id:guid}", Name = RouteNames.DeleteTag)]
    [EndpointSummary("Delete Tag")]
    [EndpointDescription("Delete a tag. Events using this tag will be unlinked.")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            var command = new DeleteTagCommand { Id = id };
            var result = await _mediator.Send(command);

            if (!result)
            {
                return NotFound(new { error = "Tag not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tag {TagId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

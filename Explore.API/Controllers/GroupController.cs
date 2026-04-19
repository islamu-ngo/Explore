// ABOUTME: REST API controller for group CRUD operations with member management and HATEOAS support.
// ABOUTME: Manages user groups, group settings, and group-level permissions.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Hateoas;
using Explore.Application.DTOs.Group;
using Explore.Application.Features.Groups.Requests.Commands;
using Explore.Application.Features.Groups.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

/// <summary>
/// Group management API endpoints.
/// All responses include HATEOAS links by default.
/// Send "Prefer: return=minimal" header to strip links.
/// </summary>
[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public class GroupController : ExploreControllerBase
{
    private readonly IMediator _mediator;
    private readonly IResourceAssembler<GroupDto, GroupListDto> _resourceAssembler;

    public GroupController(
        IMediator mediator,
        IResourceAssembler<GroupDto, GroupListDto> resourceAssembler)
    {
        _mediator = mediator;
        _resourceAssembler = resourceAssembler;
    }

    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet(Name = RouteNames.GetGroups)]
    [EndpointSummary("Get all Groups")]
    [EndpointDescription("Get a paginated list of all Groups. Default page size is 20, max is 100.")]
    [ProducesResponseType(typeof(HalCollectionResource<GroupListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<HalCollectionResource<GroupListDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetGroupListRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);

        var halResource = await _resourceAssembler.ToCollectionResource(
            result,
            RouteNames.GetGroups,
            additionalRouteValues: null,
            HttpContext);

        return Ok(halResource);
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpGet("my", Name = RouteNames.GetMyGroups)]
    [EndpointSummary("Get my Groups")]
    [EndpointDescription("Get a paginated list of groups where the current user is a member.")]
    [ProducesResponseType(typeof(HalCollectionResource<GroupListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<HalCollectionResource<GroupListDto>>> GetMyGroups(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId?.ToString();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var result = await _mediator.Send(new GetMyGroupsRequest
        {
            UserId = userId,
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);

        var halResource = await _resourceAssembler.ToCollectionResource(
            result,
            RouteNames.GetMyGroups,
            additionalRouteValues: null,
            HttpContext);

        return Ok(halResource);
    }

    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("{id:guid}", Name = RouteNames.GetGroupById)]
    [EndpointSummary("Get Group Details")]
    [EndpointDescription("Get full details of a group.")]
    [ProducesResponseType(typeof(HalResource<GroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<HalResource<GroupDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var group = await _mediator.Send(new GetGroupDetailsRequest { Id = id }, cancellationToken);

        if (group == null)
            return NotFound();

        var halResource = await _resourceAssembler.ToResource(group, HttpContext);
        return Ok(halResource);
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost(Name = RouteNames.CreateGroup)]
    [EndpointSummary("Create Group")]
    [EndpointDescription("Create a new group. The authenticated user becomes the creator.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateGroupDto group, CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new CreateGroupCommand { GroupDto = group }, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtRoute(RouteNames.GetGroupById, new { id = response.Id }, response);
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPut("{id:guid}", Name = RouteNames.UpdateGroup)]
    [EndpointSummary("Update Group")]
    [EndpointDescription("Update an existing group. User must have group management permission.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(
        Guid id,
        [FromBody] UpdateGroupDto updateDto,
        CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId?.ToString();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var command = new UpdateGroupCommand
        {
            Id = id,
            UserId = userId,
            GroupDto = updateDto
        };

        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpDelete("{id:guid}", Name = RouteNames.DeleteGroup)]
    [EndpointSummary("Delete Group")]
    [EndpointDescription("Delete a group. User must have group deletion permission.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId?.ToString();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var response = await _mediator.Send(new DeleteGroupCommand
        {
            Id = id,
            UserId = userId
        }, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

}

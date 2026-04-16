// ABOUTME: REST API controller for group member CRUD operations with role-based access control.
// ABOUTME: Manages user membership in groups and associated permissions via CQRS/MediatR.

using Asp.Versioning;
using Explore.API.Hateoas;
using Explore.Application.DTOs.GroupMember;
using Explore.Application.Features.GroupMembers.Requests.Commands;
using Explore.Application.Features.GroupMembers.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class GroupMemberController : ExploreControllerBase
{
    private readonly IMediator _mediator;

    public GroupMemberController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{groupId:guid}", Name = RouteNames.GetGroupMembers)]
    [AllowAnonymous]
    [OutputCache(PolicyName = "ListData")]
    [ProducesResponseType(typeof(List<GroupMemberDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GroupMemberDto>>> GetByGroupId(Guid groupId, CancellationToken cancellationToken = default)
    {
        var members = await _mediator.Send(new GetGroupMembersRequest { GroupId = groupId }, cancellationToken);
        return Ok(members);
    }

    [HttpGet("member/{id:guid}", Name = RouteNames.GetGroupMemberById)]
    [AllowAnonymous]
    [OutputCache(PolicyName = "DetailData")]
    [ProducesResponseType(typeof(GroupMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GroupMemberDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var member = await _mediator.Send(new GetGroupMemberDetailsRequest { Id = id }, cancellationToken);

        return Ok(member);
    }

    [HttpPost(Name = RouteNames.CreateGroupMember)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Post([FromBody] AddGroupMemberDto dto, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId?.ToString();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var command = new AddGroupMemberCommand
        {
            AddGroupMemberDto = dto,
            RequesterUserId = userId
        };

        var response = await _mediator.Send(command, cancellationToken);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPut("role", Name = RouteNames.UpdateGroupMember)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateRole([FromBody] UpdateGroupMemberRoleDto dto, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId?.ToString();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var command = new UpdateGroupMemberRoleCommand
        {
            UpdateGroupMemberRoleDto = dto,
            RequesterUserId = userId
        };

        var response = await _mediator.Send(command, cancellationToken);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpDelete("{id:guid}", Name = RouteNames.DeleteGroupMember)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId?.ToString();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var command = new DeleteGroupMemberCommand
        {
            MemberId = id,
            RequesterUserId = userId
        };

        var response = await _mediator.Send(command, cancellationToken);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

}

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
public class GroupMemberController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GroupMemberController(IMediator mediator, IHttpContextAccessor httpContextAccessor)
    {
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
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

    [HttpPost(Name = RouteNames.CreateGroupMember)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Post([FromBody] AddGroupMemberDto dto, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
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
        var userId = GetCurrentUserId();
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
        var userId = GetCurrentUserId();
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

    private string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sid")?.Value;
    }
}

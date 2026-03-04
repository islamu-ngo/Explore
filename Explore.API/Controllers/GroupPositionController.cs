// ABOUTME: API controller for GroupPosition lookup table.
// ABOUTME: Read-only endpoints matching OrganizationPositionController pattern.

using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using Explore.Application.DTOs.GroupPosition;
using Explore.Application.Features.GroupPositions.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
public class GroupPositionController : ControllerBase
{
    private readonly IMediator _mediator;

    public GroupPositionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [EndpointSummary("Get all Group Positions")]
    [EndpointDescription("Retrieve a list of all group positions (Leader, Coordinator, Member, etc.)")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<GroupPositionListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "LookupData")]
    public async Task<ActionResult<List<GroupPositionListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var groupPositions = await _mediator.Send(new GetGroupPositionListRequest(), cancellationToken);
        return Ok(groupPositions);
    }

    [HttpGet("{id}")]
    [EndpointSummary("Get Group Position by ID")]
    [EndpointDescription("Retrieve details of a specific group position")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GroupPositionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<GroupPositionDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var groupPosition = await _mediator.Send(new GetGroupPositionDetailsRequest { Id = id }, cancellationToken);
        return Ok(groupPosition);
    }
}

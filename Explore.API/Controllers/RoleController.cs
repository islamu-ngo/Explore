// ABOUTME: Unified role controller replacing OrganizationRoleController and UserRoleController.
// ABOUTME: Supports scope filtering via query parameter (Platform, Tenant, Organization).

using Asp.Versioning;
using Explore.API.Hateoas;
using Explore.Application.DTOs.Role;
using Explore.Application.Features.Roles.Requests.Queries;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
public class RoleController : ControllerBase
{
    private readonly IMediator _mediator;

    public RoleController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/v1/role?scope=Organization
    [HttpGet(Name = RouteNames.GetRoles)]
    [EndpointSummary("Get all Roles")]
    [EndpointDescription("Retrieve roles, optionally filtered by scope (Platform, Tenant, Organization). Returns all roles when no scope specified.")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<RoleListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "LookupData")]
    public async Task<ActionResult<List<RoleListDto>>> GetAll(
        [FromQuery] RoleScopeEnum? scope = null,
        CancellationToken cancellationToken = default)
    {
        var roles = await _mediator.Send(new GetRoleListRequest { Scope = scope }, cancellationToken);
        return Ok(roles);
    }

    // GET: api/v1/role/{id}
    [HttpGet("{id}", Name = RouteNames.GetRoleById)]
    [EndpointSummary("Get Role by ID")]
    [EndpointDescription("Retrieve details of a specific role including scope and system flag.")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<RoleDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var role = await _mediator.Send(new GetRoleDetailsRequest { Id = id }, cancellationToken);
        if (role == null)
        {
            return NotFound();
        }

        return Ok(role);
    }
}

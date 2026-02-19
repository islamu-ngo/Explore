using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using Explore.Application.DTOs.TenantUser;
using Explore.Application.Features.TenantUsers.Requests.Commands;
using Explore.Application.Features.TenantUsers.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
public class TenantUserController : ControllerBase
{
    private readonly IMediator _mediator;

    public TenantUserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/tenantuser
    [HttpGet]
    [EndpointSummary("Get all Tenant Users")]
    [EndpointDescription("Retrieve a list of all tenant users")]
    [Authorize]
    [ProducesResponseType(typeof(List<TenantUserListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<List<TenantUserListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var tenantUsers = await _mediator.Send(new GetTenantUserListRequest(), cancellationToken);
        return Ok(tenantUsers);
    }

    // GET: api/tenantuser/{id}
    [HttpGet("{id}")]
    [EndpointSummary("Get Tenant User by ID")]
    [EndpointDescription("Retrieve details of a specific tenant user")]
    [Authorize]
    [ProducesResponseType(typeof(TenantUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<TenantUserDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantUser = await _mediator.Send(new GetTenantUserDetailsRequest { Id = id }, cancellationToken);
        if (tenantUser == null)
        {
            return NotFound();
        }

        return Ok(tenantUser);
    }

    // POST: api/tenantuser
    [HttpPost]
    [EndpointSummary("Create new Tenant User")]
    [EndpointDescription("Create a new tenant user association")]
    [Authorize]
    [ProducesResponseType(typeof(BaseCommandResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<int>>> Create([FromBody] CreateTenantUserDto dto, CancellationToken cancellationToken = default)
    {
        var command = new CreateTenantUserCommand { TenantUserDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    // PUT: api/tenantuser/{id}
    [HttpPut("{id}")]
    [EndpointSummary("Update Tenant User")]
    [EndpointDescription("Update an existing tenant user association")]
    [Authorize]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateTenantUserDto dto, CancellationToken cancellationToken = default)
    {
        if (id != dto.Id)
        {
            return BadRequest(new { error = "Tenant User ID mismatch" });
        }

        var command = new UpdateTenantUserCommand { TenantUserDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    // DELETE: api/tenantuser/{id}
    [HttpDelete("{id}")]
    [EndpointSummary("Delete Tenant User")]
    [EndpointDescription("Delete a tenant user association")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteTenantUserCommand { Id = id };
        var result = await _mediator.Send(command, cancellationToken);

        if (!result)
        {
            return NotFound(new { error = "Tenant User not found" });
        }

        return NoContent();
    }
}

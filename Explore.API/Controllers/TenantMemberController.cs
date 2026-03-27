// ABOUTME: REST API controller for tenant member CRUD operations.
// ABOUTME: Manages user-role assignments within tenants via CQRS/MediatR.

using Asp.Versioning;
using Explore.Application.DTOs.TenantMember;
using Explore.Application.Features.TenantMembers.Requests.Commands;
using Explore.Application.Features.TenantMembers.Requests.Queries;
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
public class TenantMemberController : ControllerBase
{
    private readonly IMediator _mediator;

    public TenantMemberController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet(Name = "GetTenantMembers")]
    [EndpointSummary("Get all Tenant Members")]
    [EndpointDescription("Retrieve a list of all tenant members")]
    [Authorize]
    [ProducesResponseType(typeof(List<TenantMemberListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<List<TenantMemberListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var tenantMembers = await _mediator.Send(new GetTenantMemberListRequest(), cancellationToken);
        return Ok(tenantMembers);
    }

    [HttpGet("{id}", Name = "GetTenantMemberById")]
    [EndpointSummary("Get Tenant Member by ID")]
    [EndpointDescription("Retrieve details of a specific tenant member")]
    [Authorize]
    [ProducesResponseType(typeof(TenantMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<TenantMemberDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantMember = await _mediator.Send(new GetTenantMemberDetailsRequest { Id = id }, cancellationToken);

        return Ok(tenantMember);
    }

    [HttpPost(Name = "CreateTenantMember")]
    [EndpointSummary("Create new Tenant Member")]
    [EndpointDescription("Create a new tenant member association")]
    [Authorize]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateTenantMemberDto dto, CancellationToken cancellationToken = default)
    {
        var command = new CreateTenantMemberCommand { TenantMemberDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPut("{id}", Name = "UpdateTenantMember")]
    [EndpointSummary("Update Tenant Member")]
    [EndpointDescription("Update an existing tenant member association")]
    [Authorize]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateTenantMemberDto dto, CancellationToken cancellationToken = default)
    {
        if (id != dto.Id)
        {
            return BadRequest(new { error = "Tenant Member ID mismatch" });
        }

        var command = new UpdateTenantMemberCommand { TenantMemberDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpDelete("{id}", Name = "DeleteTenantMember")]
    [EndpointSummary("Delete Tenant Member")]
    [EndpointDescription("Delete a tenant member association")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteTenantMemberCommand { Id = id };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }
}

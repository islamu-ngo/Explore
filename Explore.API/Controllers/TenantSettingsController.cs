using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using Explore.Application.DTOs.TenantSettings;
using Explore.Application.Features.TenantSettings.Requests.Commands;
using Explore.Application.Features.TenantSettings.Requests.Queries;
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
public class TenantSettingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TenantSettingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/v1/tenantsettings
    [HttpGet]
    [EndpointSummary("Get all Tenant Settings")]
    [EndpointDescription("Retrieve a list of all tenant settings")]
    [Authorize]
    [ProducesResponseType(typeof(List<TenantSettingsListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<List<TenantSettingsListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var tenantSettings = await _mediator.Send(new GetTenantSettingsListRequest(), cancellationToken);
        return Ok(tenantSettings);
    }

    // GET: api/v1/tenantsettings/{id}
    [HttpGet("{id}")]
    [EndpointSummary("Get Tenant Settings by ID")]
    [EndpointDescription("Retrieve details of specific tenant settings")]
    [Authorize]
    [ProducesResponseType(typeof(TenantSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<TenantSettingsDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantSettings = await _mediator.Send(new GetTenantSettingsDetailsRequest { Id = id }, cancellationToken);
        if (tenantSettings == null)
        {
            return NotFound();
        }

        return Ok(tenantSettings);
    }

    // POST: api/v1/tenantsettings
    [HttpPost]
    [EndpointSummary("Create new Tenant Settings")]
    [EndpointDescription("Create new tenant settings")]
    [Authorize]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateTenantSettingsDto dto, CancellationToken cancellationToken = default)
    {
        var command = new CreateTenantSettingsCommand { TenantSettingsDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    // PUT: api/v1/tenantsettings/{id}
    [HttpPut("{id}")]
    [EndpointSummary("Update Tenant Settings")]
    [EndpointDescription("Update existing tenant settings")]
    [Authorize]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateTenantSettingsDto dto, CancellationToken cancellationToken = default)
    {
        if (id != dto.Id)
        {
            return BadRequest(new { error = "Tenant Settings ID mismatch" });
        }

        var command = new UpdateTenantSettingsCommand { TenantSettingsDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    // DELETE: api/v1/tenantsettings/{id}
    [HttpDelete("{id}")]
    [EndpointSummary("Delete Tenant Settings")]
    [EndpointDescription("Delete tenant settings")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteTenantSettingsCommand { Id = id };
        var result = await _mediator.Send(command, cancellationToken);

        if (!result)
        {
            return NotFound(new { error = "Tenant Settings not found" });
        }

        return NoContent();
    }
}

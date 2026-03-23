using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Asp.Versioning;
using Explore.Application.DTOs.Tenant;
using Explore.Application.Features.InstanceOnboarding.Requests.Queries;
using Explore.Application.Features.Tenants.Requests.Commands;
using Explore.Application.Features.Tenants.Requests.Commands.CreateTenantNavLink;
using Explore.Application.Features.Tenants.Requests.Commands.DeleteTenantNavLink;
using Explore.Application.Features.Tenants.Requests.Commands.ReorderTenantNavLinks;
using Explore.Application.Features.Tenants.Requests.Commands.UpdateTenantNavLink;
using Explore.Application.Features.Tenants.Requests.Queries;
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
public class TenantController : ControllerBase
{
    private readonly IMediator _mediator;

    public TenantController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/tenant
    [HttpGet]
    [EndpointSummary("Get all Tenants")]
    [EndpointDescription("Retrieve a list of all tenants")]
    [Authorize]
    [ProducesResponseType(typeof(List<TenantListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<List<TenantListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var tenants = await _mediator.Send(new GetTenantListRequest(), cancellationToken);
        return Ok(tenants);
    }

    // GET: api/tenant/count
    [HttpGet("count")]
    [EndpointSummary("Get Active Tenant Count")]
    [EndpointDescription("Returns the number of active tenants. Used by deployment mode toggle safeguards.")]
    [Authorize]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> GetCount(CancellationToken cancellationToken = default)
    {
        var count = await _mediator.Send(new GetActiveTenantCountQuery(), cancellationToken);
        return Ok(count);
    }

    // GET: api/tenant/{id}
    [HttpGet("{id}")]
    [EndpointSummary("Get Tenant by ID")]
    [EndpointDescription("Retrieve details of a specific tenant")]
    [Authorize]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<TenantDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await _mediator.Send(new GetTenantDetailsRequest { Id = id }, cancellationToken);
        if (tenant == null)
        {
            return NotFound();
        }

        return Ok(tenant);
    }

    // POST: api/tenant
    [HttpPost]
    [EndpointSummary("Create new Tenant")]
    [EndpointDescription("Create a new tenant")]
    [Authorize]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateTenantDto dto, CancellationToken cancellationToken = default)
    {
        var command = new CreateTenantCommand
        {
            TenantDto = dto,
            RequestingUserId = GetCurrentUserId()
        };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sid")?.Value;
        return Guid.TryParse(claim, out var userId) ? userId : null;
    }

    // PUT: api/tenant/{id}
    [HttpPut("{id}")]
    [EndpointSummary("Update Tenant")]
    [EndpointDescription("Update an existing tenant")]
    [Authorize]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateTenantDto dto, CancellationToken cancellationToken = default)
    {
        if (id != dto.Id)
        {
            return BadRequest(new { error = "Tenant ID mismatch" });
        }

        var command = new UpdateTenantCommand { TenantDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    // DELETE: api/tenant/{id}
    [HttpDelete("{id}")]
    [EndpointSummary("Delete Tenant")]
    [EndpointDescription("Delete a tenant")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteTenantCommand { Id = id };
        var result = await _mediator.Send(command, cancellationToken);

        if (!result)
        {
            return NotFound(new { error = "Tenant not found" });
        }

        return NoContent();
    }

    // GET: api/tenant/navigation
    [HttpGet("navigation")]
    [EndpointSummary("Get Tenant Navigation Links")]
    [EndpointDescription("Retrieve all navigation links for the current tenant")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<TenantNavigationLinkDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<List<TenantNavigationLinkDto>>> GetNavigation(CancellationToken cancellationToken = default)
    {
        var links = await _mediator.Send(new GetTenantNavLinksQuery(), cancellationToken);
        return Ok(links);
    }

    // POST: api/tenant/navigation
    [HttpPost("navigation")]
    [EndpointSummary("Create Tenant Navigation Link")]
    [EndpointDescription("Create a new navigation link for the tenant")]
    [Authorize]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> CreateNavigation(
        [FromBody] CreateTenantNavigationLinkDto dto,
        [FromServices] IOutputCacheStore cacheStore,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateTenantNavLinkCommand { NavigationLinkDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        // Invalidate cache
        await cacheStore.EvictByTagAsync("tenant-nav", cancellationToken);

        return Ok(response);
    }

    // PUT: api/tenant/navigation/{id}
    [HttpPut("navigation/{id}")]
    [EndpointSummary("Update Tenant Navigation Link")]
    [EndpointDescription("Update an existing navigation link")]
    [Authorize]
    [ProducesResponseType(typeof(BaseCommandResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<bool>>> UpdateNavigation(
        Guid id,
        [FromBody] UpdateTenantNavigationLinkDto dto,
        [FromServices] IOutputCacheStore cacheStore,
        CancellationToken cancellationToken = default)
    {
        if (id != dto.Id)
        {
            return BadRequest(new { error = "Navigation link ID mismatch" });
        }

        var command = new UpdateTenantNavLinkCommand { NavigationLinkDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        // Invalidate cache
        await cacheStore.EvictByTagAsync("tenant-nav", cancellationToken);

        return Ok(response);
    }

    // DELETE: api/tenant/navigation/{id}
    [HttpDelete("navigation/{id}")]
    [EndpointSummary("Delete Tenant Navigation Link")]
    [EndpointDescription("Delete a navigation link")]
    [Authorize]
    [ProducesResponseType(typeof(BaseCommandResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<bool>>> DeleteNavigation(
        Guid id,
        [FromServices] IOutputCacheStore cacheStore,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteTenantNavLinkCommand { Id = id };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return NotFound(response);
        }

        // Invalidate cache
        await cacheStore.EvictByTagAsync("tenant-nav", cancellationToken);

        return Ok(response);
    }

    // PUT: api/tenant/navigation/reorder
    [HttpPut("navigation/reorder")]
    [EndpointSummary("Reorder Tenant Navigation Links")]
    [EndpointDescription("Reorder multiple navigation links")]
    [Authorize]
    [ProducesResponseType(typeof(BaseCommandResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<bool>>> ReorderNavigation(
        [FromBody] List<UpdateTenantNavigationLinkOrderDto> orders,
        [FromServices] IOutputCacheStore cacheStore,
        CancellationToken cancellationToken = default)
    {
        var command = new ReorderTenantNavLinksCommand { NavigationLinkOrders = orders };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        // Invalidate cache
        await cacheStore.EvictByTagAsync("tenant-nav", cancellationToken);

        return Ok(response);
    }
}

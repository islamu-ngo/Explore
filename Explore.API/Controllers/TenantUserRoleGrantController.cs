// ABOUTME: REST API controller for auditable tenant user role grants.
// ABOUTME: Exposes create/revoke mutations and HAL collection/detail reads via CQRS/MediatR.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Hateoas;
using Explore.Application.DTOs.TenantUserRoleGrant;
using Explore.Application.Features.TenantUserRoleGrants.Requests.Commands;
using Explore.Application.Features.TenantUserRoleGrants.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/tenant-user-role-grants")]
[ApiController]
[Authorize]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public class TenantUserRoleGrantController : ExploreControllerBase
{
    private readonly IMediator _mediator;
    private readonly IResourceAssembler<TenantUserRoleGrantDto, TenantUserRoleGrantListDto> _resourceAssembler;

    public TenantUserRoleGrantController(
        IMediator mediator,
        IResourceAssembler<TenantUserRoleGrantDto, TenantUserRoleGrantListDto> resourceAssembler)
    {
        _mediator = mediator;
        _resourceAssembler = resourceAssembler;
    }

    [HttpGet(Name = RouteNames.GetTenantUserRoleGrants)]
    [EndpointSummary("Get all tenant user role grants")]
    [EndpointDescription("Retrieve tenant-scoped user role grants")]
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [ProducesResponseType(typeof(HalCollectionResource<TenantUserRoleGrantListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<HalCollectionResource<TenantUserRoleGrantListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var tenantUserRoleGrants = await _mediator.Send(new GetTenantUserRoleGrantListRequest(), cancellationToken);
        var halResource = await _resourceAssembler.ToCollectionResource(
            tenantUserRoleGrants,
            RouteNames.GetTenantUserRoleGrants,
            null,
            HttpContext);

        return Ok(halResource);
    }

    [HttpGet("{id:guid}", Name = RouteNames.GetTenantUserRoleGrantById)]
    [EndpointSummary("Get tenant user role grant by ID")]
    [EndpointDescription("Retrieve details of a tenant user role grant")]
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [ProducesResponseType(typeof(HalResource<TenantUserRoleGrantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<HalResource<TenantUserRoleGrantDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantUserRoleGrant = await _mediator.Send(new GetTenantUserRoleGrantDetailsRequest { Id = id }, cancellationToken);
        if (tenantUserRoleGrant is null)
        {
            return NotFound();
        }

        var halResource = await _resourceAssembler.ToResource(tenantUserRoleGrant, HttpContext);
        return Ok(halResource);
    }

    [HttpPost(Name = RouteNames.CreateTenantUserRoleGrant)]
    [EndpointSummary("Create tenant user role grant")]
    [EndpointDescription("Grant a tenant-scoped role to an existing tenant-local user")]
    [EndpointClassification(EndpointClass.Authenticated)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateTenantUserRoleGrantDto dto, CancellationToken cancellationToken = default)
    {
        var command = new CreateTenantUserRoleGrantCommand { TenantUserRoleGrantDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpDelete("{id:guid}", Name = RouteNames.RevokeTenantUserRoleGrant)]
    [EndpointSummary("Revoke tenant user role grant")]
    [EndpointDescription("Revoke an active tenant user role grant")]
    [EndpointClassification(EndpointClass.Authenticated)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Revoke(Guid id, CancellationToken cancellationToken = default)
    {
        var revoked = await _mediator.Send(new RevokeTenantUserRoleGrantCommand { Id = id }, cancellationToken);
        if (!revoked)
        {
            return NotFound();
        }

        return NoContent();
    }
}

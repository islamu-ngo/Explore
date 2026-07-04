// ABOUTME: Admin API surface for multi-tenant control-plane read models.
// ABOUTME: Gates instance-console data behind authentication, deployment mode, and HAL permissions.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Extensions;
using Explore.API.Filters;
using Explore.API.Hateoas;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.ControlPlane;
using Explore.Application.DTOs.Tenant;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Application.Features.ControlPlane.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/admin/control-plane")]
[ApiController]
[Authorize]
[RequireMultiTenant]
[EndpointClassification(EndpointClass.Admin)]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public sealed class ControlPlaneController : ExploreControllerBase
{
    private readonly IMediator _mediator;
    private readonly IResourceAssembler<ControlPlaneOverviewDto, ControlPlaneOverviewDto> _overviewAssembler;
    private readonly IResourceAssembler<ControlPlaneDomainOverviewDto, ControlPlaneDomainOverviewDto> _domainAssembler;
    private readonly IResourceAssembler<ControlPlaneOperationsDto, ControlPlaneOperationsDto> _operationsAssembler;
    private readonly IResourceAssembler<ControlPlaneTenantDetailDto, ControlPlaneTenantListItemDto> _tenantAssembler;

    public ControlPlaneController(
        IMediator mediator,
        IResourceAssembler<ControlPlaneOverviewDto, ControlPlaneOverviewDto> overviewAssembler,
        IResourceAssembler<ControlPlaneDomainOverviewDto, ControlPlaneDomainOverviewDto> domainAssembler,
        IResourceAssembler<ControlPlaneOperationsDto, ControlPlaneOperationsDto> operationsAssembler,
        IResourceAssembler<ControlPlaneTenantDetailDto, ControlPlaneTenantListItemDto> tenantAssembler)
    {
        _mediator = mediator;
        _overviewAssembler = overviewAssembler;
        _domainAssembler = domainAssembler;
        _operationsAssembler = operationsAssembler;
        _tenantAssembler = tenantAssembler;
    }

    [HttpGet("overview", Name = RouteNames.GetControlPlaneOverview)]
    [EnableRateLimiting(RateLimitingExtensions.AuthenticatedPolicy)]
    [RequestTimeout(RequestTimeoutExtensions.LookupPolicy)]
    [EndpointSummary("Get Control Plane Overview")]
    [EndpointDescription("Returns the multi-tenant instance control-plane overview for instance administrators.")]
    [ProducesResponseType(typeof(HalResource<ControlPlaneOverviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<HalResource<ControlPlaneOverviewDto>>> GetOverview(
        CancellationToken cancellationToken = default)
    {
        var overview = await _mediator.Send(new GetControlPlaneOverviewQuery(), cancellationToken);
        var resource = await _overviewAssembler.ToResource(overview, HttpContext);

        return Ok(resource);
    }

    [HttpGet("domains", Name = RouteNames.GetControlPlaneDomains)]
    [EnableRateLimiting(RateLimitingExtensions.AuthenticatedPolicy)]
    [RequestTimeout(RequestTimeoutExtensions.LookupPolicy)]
    [EndpointSummary("Get Control Plane Domains")]
    [EndpointDescription("Returns multi-tenant control-plane domain and DNS guidance for instance administrators.")]
    [ProducesResponseType(typeof(HalResource<ControlPlaneDomainOverviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<HalResource<ControlPlaneDomainOverviewDto>>> GetDomains(
        CancellationToken cancellationToken = default)
    {
        var domains = await _mediator.Send(new GetControlPlaneDomainsQuery(), cancellationToken);
        var resource = await _domainAssembler.ToResource(domains, HttpContext);

        return Ok(resource);
    }

    [HttpGet("operations", Name = RouteNames.GetControlPlaneOperations)]
    [EnableRateLimiting(RateLimitingExtensions.AuthenticatedPolicy)]
    [RequestTimeout(RequestTimeoutExtensions.LookupPolicy)]
    [EndpointSummary("Get Control Plane Operations")]
    [EndpointDescription("Returns multi-tenant control-plane operational status for jobs, outbox, email dispatch, and storage.")]
    [ProducesResponseType(typeof(HalResource<ControlPlaneOperationsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<HalResource<ControlPlaneOperationsDto>>> GetOperations(
        CancellationToken cancellationToken = default)
    {
        var operations = await _mediator.Send(new GetControlPlaneOperationsQuery(), cancellationToken);
        var resource = await _operationsAssembler.ToResource(operations, HttpContext);

        return Ok(resource);
    }

    [HttpGet("tenants", Name = RouteNames.GetControlPlaneTenants)]
    [EnableRateLimiting(RateLimitingExtensions.AuthenticatedPolicy)]
    [RequestTimeout(RequestTimeoutExtensions.LookupPolicy)]
    [EndpointSummary("Get Control Plane Tenants")]
    [EndpointDescription("Returns the multi-tenant control-plane tenant lifecycle list for instance administrators.")]
    [ProducesResponseType(typeof(HalCollectionResource<ControlPlaneTenantListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<HalCollectionResource<ControlPlaneTenantListItemDto>>> GetTenants(
        CancellationToken cancellationToken = default)
    {
        var tenants = await _mediator.Send(new GetControlPlaneTenantListQuery(), cancellationToken);
        var resource = await _tenantAssembler.ToCollectionResource(tenants, RouteNames.GetControlPlaneTenants, HttpContext);

        return Ok(resource);
    }

    [HttpGet("tenants/{tenantId:guid}", Name = RouteNames.GetControlPlaneTenantById)]
    [EnableRateLimiting(RateLimitingExtensions.AuthenticatedPolicy)]
    [RequestTimeout(RequestTimeoutExtensions.LookupPolicy)]
    [EndpointSummary("Get Control Plane Tenant")]
    [EndpointDescription("Returns one multi-tenant control-plane tenant lifecycle detail resource for instance administrators.")]
    [ProducesResponseType(typeof(HalResource<ControlPlaneTenantDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HalResource<ControlPlaneTenantDetailDto>>> GetTenantById(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _mediator.Send(new GetControlPlaneTenantDetailsQuery(tenantId), cancellationToken);
        if (tenant is null)
        {
            return NotFound();
        }

        var resource = await _tenantAssembler.ToResource(tenant, HttpContext);

        return Ok(resource);
    }

    [HttpPost("tenants", Name = RouteNames.CreateControlPlaneTenant)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.DefaultPolicy)]
    [EndpointSummary("Create Control Plane Tenant")]
    [EndpointDescription("Creates a tenant through the multi-tenant control plane.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> CreateTenant(
        [FromBody] CreateTenantDto dto,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new Explore.Application.Features.Tenants.Requests.Commands.CreateTenantCommand
        {
            TenantDto = dto,
            RequestingUserId = CurrentUserId
        }, cancellationToken);

        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("tenants/{tenantId:guid}/activate", Name = RouteNames.ActivateControlPlaneTenant)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.DefaultPolicy)]
    [EndpointSummary("Activate Control Plane Tenant")]
    [EndpointDescription("Activates a provisioning tenant through the multi-tenant control plane.")]
    [ProducesResponseType(typeof(BaseCommandResponse<ControlPlaneTenantLifecycleTransitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public Task<ActionResult<BaseCommandResponse<ControlPlaneTenantLifecycleTransitionDto>>> ActivateTenant(
        Guid tenantId,
        [FromBody] ControlPlaneTenantLifecycleTransitionRequestDto? dto,
        CancellationToken cancellationToken = default) =>
        TransitionTenant(tenantId, TenantStatusEnum.Active, dto, cancellationToken);

    [HttpPost("tenants/{tenantId:guid}/suspend", Name = RouteNames.SuspendControlPlaneTenant)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.DefaultPolicy)]
    [EndpointSummary("Suspend Control Plane Tenant")]
    [EndpointDescription("Suspends a tenant through the multi-tenant control plane.")]
    [ProducesResponseType(typeof(BaseCommandResponse<ControlPlaneTenantLifecycleTransitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public Task<ActionResult<BaseCommandResponse<ControlPlaneTenantLifecycleTransitionDto>>> SuspendTenant(
        Guid tenantId,
        [FromBody] ControlPlaneTenantLifecycleTransitionRequestDto? dto,
        CancellationToken cancellationToken = default) =>
        TransitionTenant(tenantId, TenantStatusEnum.Suspended, dto, cancellationToken);

    [HttpPost("tenants/{tenantId:guid}/archive", Name = RouteNames.ArchiveControlPlaneTenant)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.DefaultPolicy)]
    [EndpointSummary("Archive Control Plane Tenant")]
    [EndpointDescription("Archives a tenant through the multi-tenant control plane.")]
    [ProducesResponseType(typeof(BaseCommandResponse<ControlPlaneTenantLifecycleTransitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public Task<ActionResult<BaseCommandResponse<ControlPlaneTenantLifecycleTransitionDto>>> ArchiveTenant(
        Guid tenantId,
        [FromBody] ControlPlaneTenantLifecycleTransitionRequestDto? dto,
        CancellationToken cancellationToken = default) =>
        TransitionTenant(tenantId, TenantStatusEnum.Archived, dto, cancellationToken);

    [HttpPost("tenants/{tenantId:guid}/reactivate", Name = RouteNames.ReactivateControlPlaneTenant)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.DefaultPolicy)]
    [EndpointSummary("Reactivate Control Plane Tenant")]
    [EndpointDescription("Reactivates a suspended or archived tenant through the multi-tenant control plane.")]
    [ProducesResponseType(typeof(BaseCommandResponse<ControlPlaneTenantLifecycleTransitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public Task<ActionResult<BaseCommandResponse<ControlPlaneTenantLifecycleTransitionDto>>> ReactivateTenant(
        Guid tenantId,
        [FromBody] ControlPlaneTenantLifecycleTransitionRequestDto? dto,
        CancellationToken cancellationToken = default) =>
        TransitionTenant(tenantId, TenantStatusEnum.Active, dto, cancellationToken);

    [HttpPost("tenants/{tenantId:guid}/schedule-purge", Name = RouteNames.ScheduleControlPlaneTenantPurge)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.DefaultPolicy)]
    [EndpointSummary("Schedule Control Plane Tenant Purge")]
    [EndpointDescription("Records audited tenant purge intent through the multi-tenant control plane without deleting data in the request path.")]
    [ProducesResponseType(typeof(BaseCommandResponse<ControlPlaneTenantLifecycleTransitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public Task<ActionResult<BaseCommandResponse<ControlPlaneTenantLifecycleTransitionDto>>> ScheduleTenantPurge(
        Guid tenantId,
        [FromBody] ControlPlaneTenantLifecycleTransitionRequestDto? dto,
        CancellationToken cancellationToken = default) =>
        TransitionTenant(tenantId, TenantStatusEnum.Purged, dto, cancellationToken);

    private async Task<ActionResult<BaseCommandResponse<ControlPlaneTenantLifecycleTransitionDto>>> TransitionTenant(
        Guid tenantId,
        TenantStatusEnum status,
        ControlPlaneTenantLifecycleTransitionRequestDto? dto,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(
            new TransitionControlPlaneTenantLifecycleCommand(tenantId, status, dto?.Reason),
            cancellationToken);

        return response.Success ? Ok(response) : BadRequest(response);
    }
}

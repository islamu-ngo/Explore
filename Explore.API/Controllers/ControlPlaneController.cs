// ABOUTME: Admin API surface for shared Control Plane read models and fleet-only actions.
// ABOUTME: Keeps shared instance operations mode-agnostic while gating tenant-fleet endpoints.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Extensions;
using Explore.API.Filters;
using Explore.API.Hateoas;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.ControlPlane;
using Explore.Application.DTOs.Tenant;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Application.Features.ControlPlane.Requests.Queries;
using Explore.Application.Features.ControlPlane.Plans;
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
[EndpointClassification(EndpointClass.Admin)]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public sealed class ControlPlaneController : ExploreControllerBase
{
    private readonly IMediator _mediator;
    private readonly IResourceAssembler<ControlPlaneOverviewDto, ControlPlaneOverviewDto> _overviewAssembler;
    private readonly IResourceAssembler<ControlPlaneDomainOverviewDto, ControlPlaneDomainOverviewDto> _domainAssembler;
    private readonly IResourceAssembler<ControlPlaneOperationsDto, ControlPlaneOperationsDto> _operationsAssembler;
    private readonly IResourceAssembler<ControlPlaneTenantDetailDto, ControlPlaneTenantListItemDto> _tenantAssembler;
    private readonly IResourceAssembler<ControlPlaneTenantPlanDetailDto, ControlPlaneTenantPlanListItemDto> _tenantPlanAssembler;

    public ControlPlaneController(
        IMediator mediator,
        IResourceAssembler<ControlPlaneOverviewDto, ControlPlaneOverviewDto> overviewAssembler,
        IResourceAssembler<ControlPlaneDomainOverviewDto, ControlPlaneDomainOverviewDto> domainAssembler,
        IResourceAssembler<ControlPlaneOperationsDto, ControlPlaneOperationsDto> operationsAssembler,
        IResourceAssembler<ControlPlaneTenantDetailDto, ControlPlaneTenantListItemDto> tenantAssembler,
        IResourceAssembler<ControlPlaneTenantPlanDetailDto, ControlPlaneTenantPlanListItemDto> tenantPlanAssembler)
    {
        _mediator = mediator;
        _overviewAssembler = overviewAssembler;
        _domainAssembler = domainAssembler;
        _operationsAssembler = operationsAssembler;
        _tenantAssembler = tenantAssembler;
        _tenantPlanAssembler = tenantPlanAssembler;
    }

    [HttpGet("overview", Name = RouteNames.GetControlPlaneOverview)]
    [EnableRateLimiting(RateLimitingExtensions.ControlPlanePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ControlPlanePolicy)]
    [EndpointSummary("Get Control Plane Overview")]
    [EndpointDescription("Returns the instance control-plane overview for single-tenant or multi-tenant administrators.")]
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
    [RequireMultiTenant]
    [EnableRateLimiting(RateLimitingExtensions.ControlPlanePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ControlPlanePolicy)]
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
    [EnableRateLimiting(RateLimitingExtensions.ControlPlanePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ControlPlanePolicy)]
    [EndpointSummary("Get Control Plane Operations")]
    [EndpointDescription("Returns control-plane operational status for jobs, outbox, email dispatch, moderation reporting, and storage.")]
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
    [RequireMultiTenant]
    [EnableRateLimiting(RateLimitingExtensions.ControlPlanePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ControlPlanePolicy)]
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

    [HttpGet("plans", Name = RouteNames.GetControlPlaneTenantPlans)]
    [EnableRateLimiting(RateLimitingExtensions.ControlPlanePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ControlPlanePolicy)]
    [EndpointSummary("Get Control Plane Tenant Plans")]
    [EndpointDescription("Returns SaaS tenant plan templates for instance administrators.")]
    [ProducesResponseType(typeof(HalCollectionResource<ControlPlaneTenantPlanListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<HalCollectionResource<ControlPlaneTenantPlanListItemDto>>> GetTenantPlans(
        CancellationToken cancellationToken = default)
    {
        var plans = await _mediator.Send(new GetControlPlaneTenantPlanListQuery(), cancellationToken);
        var resource = await _tenantPlanAssembler.ToCollectionResource(plans, RouteNames.GetControlPlaneTenantPlans, HttpContext);

        return Ok(resource);
    }

    [HttpGet("plans/{key}", Name = RouteNames.GetControlPlaneTenantPlanByKey)]
    [EnableRateLimiting(RateLimitingExtensions.ControlPlanePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ControlPlanePolicy)]
    [EndpointSummary("Get Control Plane Tenant Plan")]
    [EndpointDescription("Returns one SaaS tenant plan template with version, setting, and quota metadata.")]
    [ProducesResponseType(typeof(HalResource<ControlPlaneTenantPlanDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HalResource<ControlPlaneTenantPlanDetailDto>>> GetTenantPlanByKey(
        string key,
        CancellationToken cancellationToken = default)
    {
        var plan = await _mediator.Send(new GetControlPlaneTenantPlanDetailQuery(key), cancellationToken);
        if (plan is null)
        {
            return NotFound();
        }

        var resource = await _tenantPlanAssembler.ToResource(plan, HttpContext);

        return Ok(resource);
    }

    [HttpPost("plans", Name = RouteNames.CreateControlPlaneTenantPlanDraft)]
    [EnableRateLimiting(RateLimitingExtensions.ControlPlanePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ControlPlanePolicy)]
    [EndpointSummary("Create Control Plane Tenant Plan Draft")]
    [EndpointDescription("Creates a draft SaaS tenant plan template for instance administrators.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> CreateTenantPlanDraft(
        [FromBody] TenantPlanDraft draft,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new CreateControlPlaneTenantPlanDraftCommand(draft), cancellationToken);

        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("plans/{key}/versions", Name = RouteNames.CreateControlPlaneTenantPlanVersionDraft)]
    [EnableRateLimiting(RateLimitingExtensions.ControlPlanePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ControlPlanePolicy)]
    [EndpointSummary("Create Control Plane Tenant Plan Version Draft")]
    [EndpointDescription("Creates a draft version for an existing SaaS tenant plan template.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> CreateTenantPlanVersionDraft(
        string key,
        [FromBody] TenantPlanDraft draft,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new CreateControlPlaneTenantPlanVersionDraftCommand(key, draft), cancellationToken);

        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPut("plans/versions/{versionId:guid}", Name = RouteNames.UpdateControlPlaneTenantPlanVersionDraft)]
    [EnableRateLimiting(RateLimitingExtensions.ControlPlanePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ControlPlanePolicy)]
    [EndpointSummary("Update Control Plane Tenant Plan Version Draft")]
    [EndpointDescription("Updates a draft SaaS tenant plan version before publication.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateTenantPlanVersionDraft(
        Guid versionId,
        [FromBody] TenantPlanDraft draft,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new UpdateControlPlaneTenantPlanVersionDraftCommand(versionId, draft), cancellationToken);

        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("plans/versions/{versionId:guid}/publish", Name = RouteNames.PublishControlPlaneTenantPlanVersion)]
    [EnableRateLimiting(RateLimitingExtensions.ControlPlanePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ControlPlanePolicy)]
    [EndpointSummary("Publish Control Plane Tenant Plan Version")]
    [EndpointDescription("Publishes a draft tenant plan version with an explicit existing-tenant assignment policy.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> PublishTenantPlanVersion(
        Guid versionId,
        [FromBody] PublishTenantPlanVersionRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(
            new PublishControlPlaneTenantPlanVersionCommand(versionId, request.ExistingTenantPolicy),
            cancellationToken);

        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("plans/versions/{versionId:guid}/archive", Name = RouteNames.ArchiveControlPlaneTenantPlanVersion)]
    [EnableRateLimiting(RateLimitingExtensions.ControlPlanePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ControlPlanePolicy)]
    [EndpointSummary("Archive Control Plane Tenant Plan Version")]
    [EndpointDescription("Archives a tenant plan version that should no longer be used for provisioning.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> ArchiveTenantPlanVersion(
        Guid versionId,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new ArchiveControlPlaneTenantPlanVersionCommand(versionId), cancellationToken);

        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("plans/versions/{sourceVersionId:guid}/clone", Name = RouteNames.CloneControlPlaneTenantPlan)]
    [EnableRateLimiting(RateLimitingExtensions.ControlPlanePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ControlPlanePolicy)]
    [EndpointSummary("Clone Control Plane Tenant Plan")]
    [EndpointDescription("Clones an existing tenant plan version into a new draft plan template.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> CloneTenantPlan(
        Guid sourceVersionId,
        [FromBody] CloneTenantPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(
            new CloneControlPlaneTenantPlanCommand(sourceVersionId, request.Key, request.Name),
            cancellationToken);

        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("plans/validate", Name = RouteNames.ValidateControlPlaneTenantPlanDraft)]
    [EnableRateLimiting(RateLimitingExtensions.ControlPlanePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ControlPlanePolicy)]
    [EndpointSummary("Validate Control Plane Tenant Plan Draft")]
    [EndpointDescription("Validates a tenant plan draft against registered settings, sensitive values, and quota rules.")]
    [ProducesResponseType(typeof(TenantPlanValidationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TenantPlanValidationResult>> ValidateTenantPlanDraft(
        [FromBody] TenantPlanDraft draft,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ValidateControlPlaneTenantPlanDraftQuery(draft), cancellationToken);

        return Ok(result);
    }

    [HttpPost("plans/preview-diff", Name = RouteNames.PreviewControlPlaneTenantPlanDiff)]
    [EnableRateLimiting(RateLimitingExtensions.ControlPlanePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ControlPlanePolicy)]
    [EndpointSummary("Preview Control Plane Tenant Plan Diff")]
    [EndpointDescription("Previews setting changes between a tenant's effective configuration and a tenant plan draft.")]
    [ProducesResponseType(typeof(TenantPlanDiffResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TenantPlanDiffResult>> PreviewTenantPlanDiff(
        [FromBody] PreviewTenantPlanDiffRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new PreviewControlPlaneTenantPlanDiffQuery(request.Current, request.Draft),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("tenants/{tenantId:guid}/plan-assignment", Name = RouteNames.GetControlPlaneTenantPlanAssignment)]
    [RequireMultiTenant]
    [EnableRateLimiting(RateLimitingExtensions.ControlPlanePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ControlPlanePolicy)]
    [EndpointSummary("Get Control Plane Tenant Plan Assignment")]
    [EndpointDescription("Returns the active tenant plan assignment for one tenant.")]
    [ProducesResponseType(typeof(ControlPlaneTenantPlanAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ControlPlaneTenantPlanAssignmentDto>> GetTenantPlanAssignment(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _mediator.Send(new GetControlPlaneTenantPlanAssignmentQuery(tenantId), cancellationToken);

        return assignment is null ? NotFound() : Ok(assignment);
    }

    [HttpPost("tenants/{tenantId:guid}/plan-assignment", Name = RouteNames.SwitchControlPlaneTenantPlanAssignment)]
    [RequireMultiTenant]
    [EnableRateLimiting(RateLimitingExtensions.ControlPlanePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ControlPlanePolicy)]
    [EndpointSummary("Switch Control Plane Tenant Plan Assignment")]
    [EndpointDescription("Switches one tenant to a selected tenant plan version without automatically applying settings.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> SwitchTenantPlanAssignment(
        Guid tenantId,
        [FromBody] SwitchTenantPlanAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var operatorId = await ResolveCurrentUserIdAsync(_mediator, cancellationToken);
        if (!operatorId.HasValue)
        {
            return this.ToAuthenticationRequiredProblem(detail: "The authenticated principal could not be resolved to an application user.");
        }

        var response = await _mediator.Send(
            new SwitchControlPlaneTenantPlanAssignmentCommand(tenantId, request.TenantPlanVersionId, operatorId.Value),
            cancellationToken);

        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("tenants/{tenantId:guid}/plan-assignments/{assignmentId:guid}/apply", Name = RouteNames.ApplyControlPlaneTenantPlanAssignment)]
    [RequireMultiTenant]
    [EnableRateLimiting(RateLimitingExtensions.ControlPlanePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ControlPlanePolicy)]
    [EndpointSummary("Apply Control Plane Tenant Plan Assignment")]
    [EndpointDescription("Explicitly applies a tenant plan assignment's settings to the tenant setting store.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> ApplyTenantPlanAssignment(
        Guid tenantId,
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        var operatorId = await ResolveCurrentUserIdAsync(_mediator, cancellationToken);
        if (!operatorId.HasValue)
        {
            return this.ToAuthenticationRequiredProblem(detail: "The authenticated principal could not be resolved to an application user.");
        }

        var response = await _mediator.Send(
            new ApplyControlPlaneTenantPlanAssignmentCommand(tenantId, assignmentId, operatorId.Value),
            cancellationToken);

        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("tenants/{tenantId:guid}/plan-assignments/{assignmentId:guid}/rollback", Name = RouteNames.RollbackControlPlaneTenantPlanAssignment)]
    [RequireMultiTenant]
    [EnableRateLimiting(RateLimitingExtensions.ControlPlanePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ControlPlanePolicy)]
    [EndpointSummary("Rollback Control Plane Tenant Plan Assignment")]
    [EndpointDescription("Rolls one tenant back to a previous tenant plan assignment.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> RollbackTenantPlanAssignment(
        Guid tenantId,
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        var operatorId = await ResolveCurrentUserIdAsync(_mediator, cancellationToken);
        if (!operatorId.HasValue)
        {
            return this.ToAuthenticationRequiredProblem(detail: "The authenticated principal could not be resolved to an application user.");
        }

        var response = await _mediator.Send(
            new RollbackControlPlaneTenantPlanAssignmentCommand(tenantId, assignmentId, operatorId.Value),
            cancellationToken);

        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpGet("tenants/{tenantId:guid}", Name = RouteNames.GetControlPlaneTenantById)]
    [RequireMultiTenant]
    [EnableRateLimiting(RateLimitingExtensions.ControlPlanePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ControlPlanePolicy)]
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
    [RequireMultiTenant]
    [EnableRateLimiting(RateLimitingExtensions.ControlPlanePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ControlPlanePolicy)]
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
    [RequireMultiTenant]
    [EnableRateLimiting(RateLimitingExtensions.ControlPlanePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ControlPlanePolicy)]
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
    [RequireMultiTenant]
    [EnableRateLimiting(RateLimitingExtensions.ControlPlanePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ControlPlanePolicy)]
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
    [RequireMultiTenant]
    [EnableRateLimiting(RateLimitingExtensions.ControlPlanePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ControlPlanePolicy)]
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
    [RequireMultiTenant]
    [EnableRateLimiting(RateLimitingExtensions.ControlPlanePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ControlPlanePolicy)]
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
    [RequireMultiTenant]
    [EnableRateLimiting(RateLimitingExtensions.ControlPlanePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ControlPlanePolicy)]
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
            new TransitionControlPlaneTenantLifecycleCommand(tenantId, status, dto?.Reason, dto?.ConfirmationText),
            cancellationToken);

        return response.Success ? Ok(response) : BadRequest(response);
    }
}

public sealed record PublishTenantPlanVersionRequest(TenantPlanExistingAssignmentPolicy ExistingTenantPolicy);

public sealed record CloneTenantPlanRequest(string Key, string Name);

public sealed record PreviewTenantPlanDiffRequest(TenantPlanEffectiveConfiguration Current, TenantPlanDraft Draft);

public sealed record SwitchTenantPlanAssignmentRequest(Guid TenantPlanVersionId);

// ABOUTME: Admin API controller for custom-property projection operations (rebuild, drain, status, governance).
// ABOUTME: Implements D2 Operability endpoints with property_governance_admin authorization, rate limiting, and request timeouts.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Extensions;
using Explore.API.Hateoas;
using Explore.Application.DTOs.CustomPropertyGovernance;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.Features.CustomPropertyGovernance.Requests.Queries;
using Explore.Application.Features.EventCustomPropertyProjections.Requests.Commands;
using Explore.Application.Features.EventCustomPropertyProjections.Requests.Queries;
using Explore.Application.Features.EventSessionCustomPropertyProjections.Requests.Commands;
using Explore.Application.Features.EventSessionCustomPropertyProjections.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/admin/custom-property-projections")]
[ApiController]
[Authorize]
[EndpointClassification(EndpointClass.Authenticated)]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public class CustomPropertyProjectionAdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IResourceAssembler<ProjectionStatusDto, ProjectionStatusDto> _statusAssembler;
    private readonly IResourceAssembler<ProjectionDirtyScopeDto, ProjectionDirtyScopeDto> _dirtyScopeAssembler;

    public CustomPropertyProjectionAdminController(
        IMediator mediator,
        IResourceAssembler<ProjectionStatusDto, ProjectionStatusDto> statusAssembler,
        IResourceAssembler<ProjectionDirtyScopeDto, ProjectionDirtyScopeDto> dirtyScopeAssembler)
    {
        _mediator = mediator;
        _statusAssembler = statusAssembler;
        _dirtyScopeAssembler = dirtyScopeAssembler;
    }

    /// <summary>
    /// Get projection status for a tenant's event custom-property projections.
    /// </summary>
    [HttpGet("status", Name = RouteNames.GetCustomPropertyProjectionStatus)]
    [EnableRateLimiting(RateLimitingExtensions.AuthenticatedPolicy)]
    [RequestTimeout(RequestTimeoutExtensions.LookupPolicy)]
    [ProducesResponseType(typeof(HalCollectionResource<ProjectionStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HalCollectionResource<ProjectionStatusDto>>> GetProjectionStatus(
        [FromQuery] Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetEventCustomPropertyProjectionStatusQuery { TenantId = tenantId },
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        var halResource = await _statusAssembler.ToCollectionResource(
            result.Id ?? [],
            RouteNames.GetCustomPropertyProjectionStatus,
            new { tenantId },
            HttpContext);

        return Ok(halResource);
    }

    /// <summary>
    /// Rebuild event custom-property projections for a tenant.
    /// </summary>
    [HttpPost("rebuild", Name = RouteNames.RebuildCustomPropertyProjection)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ComplexPolicy)]
    [ProducesResponseType(typeof(BaseCommandResponse<RebuildProjectionResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<BaseCommandResponse<RebuildProjectionResponseDto>>> RebuildProjection(
        [FromBody] RebuildProjectionRequestDto requestDto,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new RebuildEventCustomPropertyProjectionCommand { RequestDto = requestDto },
            cancellationToken);

        return result.Success ? Ok(result) : this.ToQuotaProblemOrBadRequest(result);
    }

    /// <summary>
    /// Rebuild projection rows for a single event.
    /// </summary>
    [HttpPost("rebuild-single-event", Name = RouteNames.RebuildSingleEventCustomPropertyProjection)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ComplexPolicy)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> RebuildSingleEventProjection(
        [FromBody] RebuildSingleEventProjectionRequestDto requestDto,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new RebuildSingleEventCustomPropertyProjectionCommand { EventId = requestDto.EventId },
            cancellationToken);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Drain pending dirty scopes for a projection without triggering a full rebuild.
    /// </summary>
    [HttpPost("drain-dirty-scopes", Name = RouteNames.DrainCustomPropertyProjectionDirtyScopes)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ComplexPolicy)]
    [ProducesResponseType(typeof(BaseCommandResponse<DrainDirtyScopesResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<DrainDirtyScopesResponseDto>>> DrainDirtyScopes(
        [FromBody] DrainDirtyScopesRequestDto requestDto,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new DrainCustomPropertyProjectionDirtyScopesCommand { RequestDto = requestDto },
            cancellationToken);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Get pending dirty-scope backlog rows for operator inspection.
    /// </summary>
    [HttpGet("dirty-scopes", Name = RouteNames.GetCustomPropertyProjectionDirtyScopes)]
    [EnableRateLimiting(RateLimitingExtensions.AuthenticatedPolicy)]
    [RequestTimeout(RequestTimeoutExtensions.LookupPolicy)]
    [ProducesResponseType(typeof(HalCollectionResource<ProjectionDirtyScopeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<HalCollectionResource<ProjectionDirtyScopeDto>>> GetDirtyScopes(
        [FromQuery] Guid tenantId,
        [FromQuery] string projectionName,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetCustomPropertyProjectionDirtyScopesQuery
            {
                TenantId = tenantId,
                ProjectionName = projectionName,
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            cancellationToken);

        var halResource = await _dirtyScopeAssembler.ToCollectionResource(
            result,
            RouteNames.GetCustomPropertyProjectionDirtyScopes,
            new { tenantId, projectionName },
            HttpContext);

        return Ok(halResource);
    }

    /// <summary>
    /// Get projection rows for a specific event.
    /// </summary>
    [HttpGet("events/{eventId:guid}", Name = RouteNames.GetCustomPropertyProjectionsForEvent)]
    [EnableRateLimiting(RateLimitingExtensions.AuthenticatedPolicy)]
    [RequestTimeout(RequestTimeoutExtensions.LookupPolicy)]
    [ProducesResponseType(typeof(BaseCommandResponse<IReadOnlyList<EventCustomPropertyProjectionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<IReadOnlyList<EventCustomPropertyProjectionDto>>>> GetProjectionsForEvent(
        Guid eventId,
        [FromQuery] ExposureLevel? exposureCeiling = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetEventCustomPropertyProjectionsForEventQuery
            {
                EventId = eventId,
                ExposureCeiling = exposureCeiling
            },
            cancellationToken);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ── Session projection endpoints ───────────────────────────────────────

    /// <summary>
    /// Get session projection status for a tenant.
    /// </summary>
    [HttpGet("sessions/status", Name = RouteNames.GetSessionCustomPropertyProjectionStatus)]
    [EnableRateLimiting(RateLimitingExtensions.AuthenticatedPolicy)]
    [RequestTimeout(RequestTimeoutExtensions.LookupPolicy)]
    [ProducesResponseType(typeof(HalCollectionResource<ProjectionStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HalCollectionResource<ProjectionStatusDto>>> GetSessionProjectionStatus(
        [FromQuery] Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetEventSessionCustomPropertyProjectionStatusQuery { TenantId = tenantId },
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        var halResource = await _statusAssembler.ToCollectionResource(
            result.Id ?? [],
            RouteNames.GetSessionCustomPropertyProjectionStatus,
            new { tenantId },
            HttpContext);

        return Ok(halResource);
    }

    /// <summary>
    /// Rebuild event session custom-property projections for a tenant.
    /// </summary>
    [HttpPost("sessions/rebuild", Name = RouteNames.RebuildSessionCustomPropertyProjection)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ComplexPolicy)]
    [ProducesResponseType(typeof(BaseCommandResponse<RebuildProjectionResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<BaseCommandResponse<RebuildProjectionResponseDto>>> RebuildSessionProjection(
        [FromBody] RebuildProjectionRequestDto requestDto,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new RebuildEventSessionCustomPropertyProjectionCommand { RequestDto = requestDto },
            cancellationToken);

        return result.Success ? Ok(result) : this.ToQuotaProblemOrBadRequest(result);
    }

    /// <summary>
    /// Rebuild projection rows for a single event session.
    /// </summary>
    [HttpPost("sessions/rebuild-single", Name = RouteNames.RebuildSingleSessionCustomPropertyProjection)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ComplexPolicy)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> RebuildSingleSessionProjection(
        [FromBody] RebuildSingleEventSessionProjectionRequestDto requestDto,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new RebuildSingleEventSessionCustomPropertyProjectionCommand { EventSessionId = requestDto.EventSessionId },
            cancellationToken);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Get projection rows for a specific event session.
    /// </summary>
    [HttpGet("sessions/{eventSessionId:guid}", Name = RouteNames.GetCustomPropertyProjectionsForSession)]
    [EnableRateLimiting(RateLimitingExtensions.AuthenticatedPolicy)]
    [RequestTimeout(RequestTimeoutExtensions.LookupPolicy)]
    [ProducesResponseType(typeof(BaseCommandResponse<IReadOnlyList<EventSessionCustomPropertyProjectionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<IReadOnlyList<EventSessionCustomPropertyProjectionDto>>>> GetProjectionsForSession(
        Guid eventSessionId,
        [FromQuery] ExposureLevel? exposureCeiling = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetEventSessionCustomPropertyProjectionsForSessionQuery
            {
                EventSessionId = eventSessionId,
                ExposureCeiling = exposureCeiling
            },
            cancellationToken);

        return result.Success ? Ok(result) : BadRequest(result);
    }
}

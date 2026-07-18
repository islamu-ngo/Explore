// ABOUTME: Admin API controller for operator-safe Basic Dispatch Mode email dispatch status.
// ABOUTME: Exposes sanitized lifecycle fields without email recipient, body, subject, or raw provider errors.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Extensions;
using Explore.API.Hateoas;
using Explore.API.Models;
using Explore.Application.DTOs.EmailDispatch;
using Explore.Application.Features.EmailDispatch;
using Explore.Application.Features.EmailDispatch.Requests.Commands;
using Explore.Application.Features.EmailDispatch.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/admin/email-dispatch")]
[ApiController]
[Authorize]
[EndpointClassification(EndpointClass.Authenticated)]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public sealed class EmailDispatchAdminController : ExploreControllerBase
{
    private readonly IMediator _mediator;
    private readonly IResourceAssembler<EmailDispatchStatusDto, EmailDispatchStatusDto> _statusAssembler;

    public EmailDispatchAdminController(
        IMediator mediator,
        IResourceAssembler<EmailDispatchStatusDto, EmailDispatchStatusDto> statusAssembler)
    {
        _mediator = mediator;
        _statusAssembler = statusAssembler;
    }

    /// <summary>
    /// Get sanitized Basic Dispatch Mode status rows for a tenant.
    /// </summary>
    [HttpGet("status", Name = RouteNames.GetEmailDispatchStatus)]
    [EnableRateLimiting(RateLimitingExtensions.AuthenticatedPolicy)]
    [RequestTimeout(RequestTimeoutExtensions.LookupPolicy)]
    [ProducesResponseType(typeof(HalCollectionResource<EmailDispatchStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<HalCollectionResource<EmailDispatchStatusDto>>> GetStatus(
        [FromQuery] EmailDispatchStatusQueryRequest query,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetEmailDispatchStatusQuery { TenantId = query.TenantId, Limit = query.Limit },
            cancellationToken);

        if (!result.Success)
        {
            return this.ToEmailDispatchValidationProblem(
                result.Message ?? "Email dispatch status query failed.",
                result.Errors);
        }

        var resource = _statusAssembler.ToCollectionResource(
            result.Id ?? [],
            RouteNames.GetEmailDispatchStatus,
            new { tenantId = query.TenantId, limit = query.Limit },
            HttpContext);

        return Ok(resource);
    }

    /// <summary>
    /// Pause Basic Dispatch Mode email delivery for one tenant.
    /// </summary>
    [HttpPut("tenants/{tenantId:guid}/pause", Name = RouteNames.PauseEmailDispatchTenant)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ComplexPolicy)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> PauseTenant(
        Guid tenantId,
        [FromQuery] EmailDispatchPauseTenantQueryRequest query,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new SetEmailDispatchTenantPauseStateCommand
            {
                TenantId = tenantId,
                IsPaused = true,
                PauseReason = query.GetNormalizedReason(),
                ChangedBy = CurrentUserId
            },
            cancellationToken);

        return result.Success ? Ok(result) : this.ToEmailDispatchProblem(result);
    }

    /// <summary>
    /// Resume Basic Dispatch Mode email delivery for one tenant.
    /// </summary>
    [HttpDelete("tenants/{tenantId:guid}/pause", Name = RouteNames.ResumeEmailDispatchTenant)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ComplexPolicy)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> ResumeTenant(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new SetEmailDispatchTenantPauseStateCommand
            {
                TenantId = tenantId,
                IsPaused = false,
                ChangedBy = CurrentUserId
            },
            cancellationToken);

        return result.Success ? Ok(result) : this.ToEmailDispatchProblem(result);
    }

    /// <summary>
    /// Park one unsafe EmailDispatch outbox row for operator review.
    /// </summary>
    [HttpPut("tenants/{tenantId:guid}/outbox/{outboxId:guid}/park", Name = RouteNames.ParkEmailDispatch)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ComplexPolicy)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> ParkDispatch(
        Guid tenantId,
        Guid outboxId,
        [FromQuery] EmailDispatchParkQueryRequest query,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new ParkEmailDispatchCommand
            {
                TenantId = tenantId,
                OutboxId = outboxId,
                Reason = query.GetNormalizedReason(),
                ChangedBy = CurrentUserId
            },
            cancellationToken);

        return result.Success ? Ok(result) : this.ToEmailDispatchProblem(result);
    }

    /// <summary>
    /// Replay one deferred EmailDispatch outbox row by resetting durable PostgreSQL state.
    /// </summary>
    [HttpPost("tenants/{tenantId:guid}/outbox/{outboxId:guid}/replay", Name = RouteNames.ReplayEmailDispatch)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ComplexPolicy)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> ReplayDispatch(
        Guid tenantId,
        Guid outboxId,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new ReplayEmailDispatchCommand
            {
                TenantId = tenantId,
                OutboxId = outboxId,
                ChangedBy = CurrentUserId
            },
            cancellationToken);

        return result.Success ? Ok(result) : this.ToEmailDispatchProblem(result);
    }

    /// <summary>
    /// Resolve one deferred EmailDispatch row without replaying its content.
    /// </summary>
    [HttpPost("tenants/{tenantId:guid}/outbox/{outboxId:guid}/resolve-without-replay", Name = RouteNames.ResolveEmailDispatchWithoutReplay)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ComplexPolicy)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> ResolveWithoutReplay(
        Guid tenantId,
        Guid outboxId,
        [FromQuery] EmailDispatchResolveQueryRequest query,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new ResolveEmailDispatchWithoutReplayCommand
            {
                TenantId = tenantId,
                OutboxId = outboxId,
                Reason = query.GetNormalizedReason(),
                ChangedBy = CurrentUserId
            },
            cancellationToken);

        return result.Success ? Ok(result) : this.ToEmailDispatchProblem(result);
    }
}

// ABOUTME: Admin API controller for operator-safe Basic Dispatch Mode email dispatch status.
// ABOUTME: Exposes sanitized lifecycle fields without email recipient, body, subject, or raw provider errors.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Extensions;
using Explore.API.Hateoas;
using Explore.Application.DTOs.EmailDispatch;
using Explore.Application.Features.EmailDispatch.Requests.Commands;
using Explore.Application.Features.EmailDispatch.Requests.Queries;
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
[Produces(HateoasConstants.JsonMediaType)]
public sealed class EmailDispatchAdminController : ExploreControllerBase
{
    private readonly IMediator _mediator;

    public EmailDispatchAdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get sanitized Basic Dispatch Mode status rows for a tenant.
    /// </summary>
    [HttpGet("status", Name = RouteNames.GetEmailDispatchStatus)]
    [EnableRateLimiting(RateLimitingExtensions.AuthenticatedPolicy)]
    [RequestTimeout(RequestTimeoutExtensions.LookupPolicy)]
    [ProducesResponseType(typeof(BaseCommandResponse<IReadOnlyList<EmailDispatchStatusDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<IReadOnlyList<EmailDispatchStatusDto>>>> GetStatus(
        [FromQuery] Guid tenantId,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetEmailDispatchStatusQuery { TenantId = tenantId, Limit = limit },
            cancellationToken);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Pause Basic Dispatch Mode email delivery for one tenant.
    /// </summary>
    [HttpPut("tenants/{tenantId:guid}/pause", Name = RouteNames.PauseEmailDispatchTenant)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ComplexPolicy)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> PauseTenant(
        Guid tenantId,
        [FromQuery] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new SetEmailDispatchTenantPauseStateCommand
            {
                TenantId = tenantId,
                IsPaused = true,
                PauseReason = reason,
                ChangedBy = CurrentUserId
            },
            cancellationToken);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Resume Basic Dispatch Mode email delivery for one tenant.
    /// </summary>
    [HttpDelete("tenants/{tenantId:guid}/pause", Name = RouteNames.ResumeEmailDispatchTenant)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ComplexPolicy)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
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

        return result.Success ? Ok(result) : BadRequest(result);
    }
}

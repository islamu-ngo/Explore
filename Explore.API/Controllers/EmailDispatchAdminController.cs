// ABOUTME: Admin API controller for operator-safe Basic Dispatch Mode email dispatch status.
// ABOUTME: Exposes sanitized lifecycle fields without email recipient, body, subject, or raw provider errors.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Extensions;
using Explore.API.Hateoas;
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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/admin/email-dispatch")]
[ApiController]
[Authorize]
[EndpointClassification(EndpointClass.Authenticated)]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public sealed class EmailDispatchAdminController : ExploreControllerBase
{
    private static readonly JsonSerializerOptions ProblemJsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HalCollectionResource<EmailDispatchStatusDto>>> GetStatus(
        [FromQuery] Guid tenantId,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetEmailDispatchStatusQuery { TenantId = tenantId, Limit = limit },
            cancellationToken);

        if (!result.Success)
        {
            return ToEmailDispatchValidationProblem(
                result.Message ?? "Email dispatch status query failed.",
                result.Errors);
        }

        var resource = _statusAssembler.ToCollectionResource(
            result.Id ?? [],
            RouteNames.GetEmailDispatchStatus,
            new { tenantId, limit },
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

    /// <summary>
    /// Park one unsafe EmailDispatch outbox row for operator review.
    /// </summary>
    [HttpPut("tenants/{tenantId:guid}/outbox/{outboxId:guid}/park", Name = RouteNames.ParkEmailDispatch)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ComplexPolicy)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> ParkDispatch(
        Guid tenantId,
        Guid outboxId,
        [FromQuery] string reason,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new ParkEmailDispatchCommand
            {
                TenantId = tenantId,
                OutboxId = outboxId,
                Reason = reason,
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

    private ActionResult ToEmailDispatchProblem(BaseCommandResponse<Guid> response)
    {
        var statusCode = response.FailureCode switch
        {
            EmailDispatchFailureCodes.NotFound => StatusCodes.Status404NotFound,
            EmailDispatchFailureCodes.InvalidTransition => StatusCodes.Status409Conflict,
            EmailDispatchFailureCodes.ConcurrentTransition => StatusCodes.Status409Conflict,
            EmailDispatchFailureCodes.Misconfigured => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status400BadRequest
        };

        ProblemDetails problemDetails = statusCode == StatusCodes.Status400BadRequest
            ? CreateEmailDispatchValidationDetails(
                response.Message ?? "Email dispatch command failed.",
                response.Errors)
            : new ProblemDetails();

        problemDetails.Status = statusCode;
        problemDetails.Title = statusCode switch
        {
            StatusCodes.Status404NotFound => "Email dispatch row not found",
            StatusCodes.Status409Conflict => "Email dispatch state transition conflict",
            StatusCodes.Status503ServiceUnavailable => "Email dispatch is misconfigured",
            _ => "Email dispatch validation failed"
        };
        problemDetails.Type = statusCode switch
        {
            StatusCodes.Status404NotFound => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
            StatusCodes.Status409Conflict => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
            StatusCodes.Status503ServiceUnavailable => "https://tools.ietf.org/html/rfc9110#section-15.6.4",
            _ => "https://tools.ietf.org/html/rfc9110#section-15.5.1"
        };
        problemDetails.Detail = response.Message ?? "Email dispatch command failed.";
        problemDetails.Instance = HttpContext.Request.Path;

        if (!string.IsNullOrWhiteSpace(response.FailureCode))
        {
            problemDetails.Extensions["code"] = response.FailureCode;
        }

        problemDetails.Extensions["traceId"] = HttpContext.TraceIdentifier;
        problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;
        problemDetails.Extensions["correlationId"] = HttpContext.Items["CorrelationId"] as string;

        return new ContentResult
        {
            StatusCode = statusCode,
            ContentType = "application/problem+json",
            Content = JsonSerializer.Serialize(problemDetails, ProblemJsonSerializerOptions)
        };
    }

    private ActionResult ToEmailDispatchValidationProblem(string detail, IReadOnlyCollection<string>? errors)
    {
        var problemDetails = CreateEmailDispatchValidationDetails(detail, errors);

        return new ContentResult
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentType = "application/problem+json",
            Content = JsonSerializer.Serialize(problemDetails, ProblemJsonSerializerOptions)
        };
    }

    private ValidationProblemDetails CreateEmailDispatchValidationDetails(
        string detail,
        IReadOnlyCollection<string>? errors)
    {
        var problemDetails = new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["emailDispatch"] = (errors is { Count: > 0 }
                ? errors
                : [detail]).ToArray()
        })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Email dispatch validation failed",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Detail = detail,
            Instance = HttpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = HttpContext.TraceIdentifier;
        problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;
        problemDetails.Extensions["correlationId"] = HttpContext.Items["CorrelationId"] as string;

        return problemDetails;
    }
}

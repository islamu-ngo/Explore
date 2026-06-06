// ABOUTME: REST API controller for event registration CRUD operations with approval workflow support.
// ABOUTME: Manages user registrations, waitlists, approval status, and registration limits per session.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Hateoas;
using Explore.API.Models;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Features.EventRegistrations.Requests.Commands;
using Explore.Application.Features.EventRegistrations.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
public class EventRegistrationController : ExploreControllerBase
{
    private const string ValidationFailedCode = "validation_failed";
    private const string ResourceNotFoundCode = "resource_not_found";

    private readonly IMediator _mediator;
    private readonly ILogger<EventRegistrationController> _logger;

    public EventRegistrationController(IMediator mediator, ILogger<EventRegistrationController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // GET: api/eventregistration
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet(Name = RouteNames.GetEventRegistrations)]
    [EndpointSummary("Get all Event Registrations")]
    [EndpointDescription("Retrieve a paginated list of all event registrations across all sessions. Default page size is 20, max is 100.")]
    [ProducesResponseType(typeof(PaginatedResult<EventRegistrationListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<PaginatedResult<EventRegistrationListDto>>> GetAll(
        [FromQuery] PaginationQueryRequest query,
        CancellationToken cancellationToken = default)
    {
        var eventRegistrations = await _mediator.Send(new GetEventRegistrationListRequest
        {
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        }, cancellationToken);
        return Ok(eventRegistrations);
    }

    // GET: api/eventregistration/{id}
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("{id}", Name = RouteNames.GetEventRegistrationById)]
    [EndpointSummary("Get Event Registration by ID")]
    [EndpointDescription("Retrieve details of a specific event registration including approval status")]
    [ProducesResponseType(typeof(EventRegistrationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<EventRegistrationDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var eventRegistration = await _mediator.Send(new GetEventRegistrationDetailsRequest { Id = id }, cancellationToken);
        return Ok(eventRegistration);
    }

    // GET: api/eventregistration/by-session/{eventSessionId}
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("by-session/{eventSessionId}", Name = RouteNames.GetRegistrationsBySession)]
    [EndpointSummary("Get Registrations by Event Session")]
    [EndpointDescription("Retrieve all user registrations for a specific event session")]
    [ProducesResponseType(typeof(List<EventRegistrationListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<List<EventRegistrationListDto>>> GetRegistrationsBySession(Guid eventSessionId, CancellationToken cancellationToken = default)
    {
        var registrations = await _mediator.Send(new GetRegistrationsBySessionRequest { EventSessionId = eventSessionId }, cancellationToken);
        return Ok(registrations);
    }

    // GET: api/eventregistration/by-user/{userId}
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("by-user/{userId}", Name = RouteNames.GetRegistrationsByUser)]
    [EndpointSummary("Get Registrations by User")]
    [EndpointDescription("Retrieve all event registrations for a specific user")]
    [ProducesResponseType(typeof(List<EventRegistrationListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EventRegistrationListDto>>> GetRegistrationsByUser(Guid userId, CancellationToken cancellationToken = default)
    {
        var registrations = await _mediator.Send(new GetRegistrationsByUserRequest { UserId = userId }, cancellationToken);
        return Ok(registrations);
    }

    // POST: api/eventregistration
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost(Name = RouteNames.CreateEventRegistration)]
    [EndpointSummary("Register User for Event Session")]
    [EndpointDescription("Create a new event registration (may require approval depending on registration mode)")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventRegistrationDto? dto, CancellationToken cancellationToken = default)
    {
        if (dto is null)
        {
            return Problem(
                type: "https://www.rfc-editor.org/rfc/rfc9110#name-400-bad-request",
                title: "Bad request",
                detail: "A registration payload is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (CurrentUserId is not { } currentUserId)
        {
            return Problem(
                type: "https://www.rfc-editor.org/rfc/rfc9110#name-401-unauthorized",
                title: "Unauthorized",
                detail: "A valid authenticated user is required.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        dto.UserId = currentUserId;

        var command = new CreateEventRegistrationCommand { EventRegistrationDto = dto };
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    // PUT: api/eventregistration/{id}
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPut("{id}", Name = RouteNames.UpdateEventRegistration)]
    [EndpointSummary("Update Event Registration")]
    [EndpointDescription("Update an existing event registration (e.g., change approval status)")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateEventRegistrationDto dto, CancellationToken cancellationToken = default)
    {
        if (id != dto.Id)
        {
            return ToValidationProblem(
                new BaseCommandResponse<Guid>
                {
                    Success = false,
                    Message = "Event registration ID mismatch.",
                    FailureCode = ValidationFailedCode,
                    Errors = ["Event registration ID mismatch."]
                },
                "Event registration ID mismatch.");
        }

        var command = new UpdateEventRegistrationCommand { EventRegistrationDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return string.Equals(response.Message, "Event Registration not found.", StringComparison.Ordinal)
                ? ToNotFoundProblem(response, "Event registration not found.")
                : ToValidationProblem(response, "Event registration update failed.");
        }

        return Ok(response);
    }

    // DELETE: api/eventregistration/{id}
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpDelete("{id}", Name = RouteNames.DeleteEventRegistration)]
    [EndpointSummary("Cancel Event Registration")]
    [EndpointDescription("Delete/cancel a user's event registration")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteEventRegistrationCommand { Id = id };
        var result = await _mediator.Send(command, cancellationToken);

        if (!result)
        {
            return Problem(detail: "Event Registration not found", statusCode: StatusCodes.Status404NotFound, title: "Resource not found");
        }

        return NoContent();
    }

    private ActionResult ToValidationProblem<TKey>(BaseCommandResponse<TKey> response, string fallbackDetail)
    {
        var errors = response.Errors is { Count: > 0 }
            ? response.Errors.ToArray()
            : [response.Message ?? fallbackDetail];

        var problemDetails = new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["eventRegistration"] = errors
        })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Event registration validation failed",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Detail = response.Message ?? fallbackDetail,
            Instance = HttpContext.Request.Path
        };

        problemDetails.Extensions["code"] = string.IsNullOrWhiteSpace(response.FailureCode)
            ? ValidationFailedCode
            : response.FailureCode;
        AddProblemDetailsExtensions(problemDetails);

        return new ObjectResult(problemDetails)
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentTypes = { "application/problem+json" }
        };
    }

    private ActionResult ToNotFoundProblem<TKey>(BaseCommandResponse<TKey> response, string fallbackDetail)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Event registration not found",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
            Detail = response.Message ?? fallbackDetail,
            Instance = HttpContext.Request.Path
        };

        problemDetails.Extensions["code"] = string.IsNullOrWhiteSpace(response.FailureCode)
            ? ResourceNotFoundCode
            : response.FailureCode;
        AddProblemDetailsExtensions(problemDetails);

        return new ObjectResult(problemDetails)
        {
            StatusCode = StatusCodes.Status404NotFound,
            ContentTypes = { "application/problem+json" }
        };
    }

    private void AddProblemDetailsExtensions(ProblemDetails problemDetails)
    {
        problemDetails.Extensions["traceId"] = HttpContext.TraceIdentifier;
        problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;

        if (HttpContext.Items["CorrelationId"] is string correlationId)
        {
            problemDetails.Extensions["correlationId"] = correlationId;
        }
    }
}

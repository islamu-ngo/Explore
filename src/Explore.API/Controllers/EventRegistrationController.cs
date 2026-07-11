// ABOUTME: REST API controller for event registration CRUD operations with approval workflow support.
// ABOUTME: Manages user registrations, waitlists, approval status, and registration limits per session.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Hateoas;
using Explore.API.Models;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Features.EventRegistrations.Requests.Commands;
using Explore.Application.Features.EventRegistrations.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
public class EventRegistrationController : ExploreControllerBase
{
    private static readonly ApiValidationProblemDescriptor CreateValidationProblem = new(
        "eventRegistration",
        "Event registration validation failed",
        "Event registration creation failed.");

    private static readonly ApiValidationProblemDescriptor UpdateValidationProblem = new(
        "eventRegistration",
        "Event registration validation failed",
        "Event registration update failed.");

    private static readonly ApiNotFoundProblemDescriptor RegistrationNotFoundProblem = new(
        "Event registration not found",
        "Event registration not found.");

    private readonly IMediator _mediator;
    private readonly ILogger<EventRegistrationController> _logger;

    public EventRegistrationController(IMediator mediator, ILogger<EventRegistrationController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // GET: api/eventregistration
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpGet(Name = RouteNames.GetEventRegistrations)]
    [EndpointSummary("Get current user's Event Registrations")]
    [EndpointDescription("Retrieve a paginated list of the authenticated user's event registrations. Default page size is 20, max is 100.")]
    [ProducesResponseType(typeof(PaginatedResult<EventRegistrationListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
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
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpGet("{id}", Name = RouteNames.GetEventRegistrationById)]
    [EndpointSummary("Get Event Registration by ID")]
    [EndpointDescription("Retrieve details of the authenticated user's specific event registration including approval status.")]
    [ProducesResponseType(typeof(EventRegistrationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventRegistrationDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var eventRegistration = await _mediator.Send(new GetEventRegistrationDetailsRequest { Id = id }, cancellationToken);
        if (eventRegistration is null)
        {
            return this.ToNotFoundProblem(RegistrationNotFoundProblem);
        }

        return Ok(eventRegistration);
    }

    // GET: api/eventregistration/by-session/{eventSessionId}
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpGet("by-session/{eventSessionId}", Name = RouteNames.GetRegistrationsBySession)]
    [EndpointSummary("Get current user's Registration by Event Session")]
    [EndpointDescription("Retrieve the authenticated user's registration for a specific event session.")]
    [ProducesResponseType(typeof(List<EventRegistrationListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<EventRegistrationListDto>>> GetRegistrationsBySession(Guid eventSessionId, CancellationToken cancellationToken = default)
    {
        var registrations = await _mediator.Send(new GetRegistrationsBySessionRequest { EventSessionId = eventSessionId }, cancellationToken);
        return Ok(registrations);
    }

    // GET: api/eventregistration/by-user/{userId}
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpGet("by-user/{userId}", Name = RouteNames.GetRegistrationsByUser)]
    [EndpointSummary("Get Registrations by User")]
    [EndpointDescription("Retrieve registrations only when the route user matches the authenticated user.")]
    [ProducesResponseType(typeof(List<EventRegistrationListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<EventRegistrationListDto>>> GetRegistrationsByUser(Guid userId, CancellationToken cancellationToken = default)
    {
        if (CurrentUserId is not { } currentUserId)
        {
            return this.ToAuthenticationRequiredProblem(detail: "A valid authenticated user is required.");
        }

        if (userId != currentUserId)
        {
            return this.ToForbiddenProblem(detail: "You can only view your own event registrations.");
        }

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
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventRegistrationDto? dto, CancellationToken cancellationToken = default)
    {
        if (dto is null)
        {
            return this.ToValidationProblem(
                CreateValidationProblem,
                "A registration payload is required.");
        }

        if (CurrentUserId is not { } currentUserId)
        {
            return this.ToAuthenticationRequiredProblem(detail: "A valid authenticated user is required.");
        }

        dto.UserId = currentUserId;

        var command = new CreateEventRegistrationCommand { EventRegistrationDto = dto };
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPatch("{id:guid}", Name = RouteNames.UpdateEventRegistration)]
    [EndpointSummary("Update Event Registration")]
    [EndpointDescription("Update an existing event registration (e.g., change approval status)")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(
        Guid id,
        [FromBody] UpdateEventRegistrationDto dto,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseConcurrencyStamp(ifMatch, out var expectedConcurrencyStamp))
        {
            return this.ToValidationProblem(
                UpdateValidationProblem,
                "If-Match header is required and must contain the current event registration concurrency stamp.");
        }

        var command = new UpdateEventRegistrationCommand
        {
            EventRegistrationId = id,
            ExpectedConcurrencyStamp = expectedConcurrencyStamp,
            EventRegistrationDto = dto
        };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return string.Equals(response.Message, "Event Registration not found.", StringComparison.Ordinal)
                ? this.ToNotFoundProblem(RegistrationNotFoundProblem)
                : this.ToCommandValidationProblem(response, UpdateValidationProblem);
        }

        return Ok(response);
    }

    private static bool TryParseConcurrencyStamp(string? ifMatch, out Guid concurrencyStamp)
    {
        concurrencyStamp = Guid.Empty;
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return false;
        }

        var trimmed = ifMatch.Trim();
        if (trimmed.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
        {
            trimmed = trimmed[1..^1];
        }

        return Guid.TryParse(trimmed, out concurrencyStamp) && concurrencyStamp != Guid.Empty;
    }

    // DELETE: api/eventregistration/{id}
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpDelete("{id}", Name = RouteNames.DeleteEventRegistration)]
    [EndpointSummary("Cancel Event Registration")]
    [EndpointDescription("Delete/cancel a user's event registration")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteEventRegistrationCommand { Id = id };
        var result = await _mediator.Send(command, cancellationToken);

        if (!result)
        {
            return this.ToNotFoundProblem(RegistrationNotFoundProblem);
        }

        return NoContent();
    }
}

// ABOUTME: REST API controller for event registration CRUD operations with approval workflow support.
// ABOUTME: Manages user registrations, waitlists, approval status, and registration limits per session.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Asp.Versioning;
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
public class EventRegistrationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<EventRegistrationController> _logger;

    public EventRegistrationController(IMediator mediator, ILogger<EventRegistrationController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // GET: api/eventregistration
    [HttpGet]
    [EndpointSummary("Get all Event Registrations")]
    [EndpointDescription("Retrieve a paginated list of all event registrations across all sessions. Default page size is 20, max is 100.")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedResult<EventRegistrationListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<PaginatedResult<EventRegistrationListDto>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var eventRegistrations = await _mediator.Send(new GetEventRegistrationListRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);
        return Ok(eventRegistrations);
    }

    // GET: api/eventregistration/{id}
    [HttpGet("{id}")]
    [EndpointSummary("Get Event Registration by ID")]
    [EndpointDescription("Retrieve details of a specific event registration including approval status")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventRegistrationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<EventRegistrationDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var eventRegistration = await _mediator.Send(new GetEventRegistrationDetailsRequest { Id = id }, cancellationToken);
        return Ok(eventRegistration);
    }

    // GET: api/eventregistration/by-session/{eventSessionId}
    [HttpGet("by-session/{eventSessionId}")]
    [EndpointSummary("Get Registrations by Event Session")]
    [EndpointDescription("Retrieve all user registrations for a specific event session")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<EventRegistrationListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<List<EventRegistrationListDto>>> GetRegistrationsBySession(Guid eventSessionId, CancellationToken cancellationToken = default)
    {
        var registrations = await _mediator.Send(new GetRegistrationsBySessionRequest { EventSessionId = eventSessionId }, cancellationToken);
        return Ok(registrations);
    }

    // GET: api/eventregistration/by-user/{userId}
    [HttpGet("by-user/{userId}")]
    [EndpointSummary("Get Registrations by User")]
    [EndpointDescription("Retrieve all event registrations for a specific user")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<EventRegistrationListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EventRegistrationListDto>>> GetRegistrationsByUser(Guid userId, CancellationToken cancellationToken = default)
    {
        var registrations = await _mediator.Send(new GetRegistrationsByUserRequest { UserId = userId }, cancellationToken);
        return Ok(registrations);
    }

    // POST: api/eventregistration
    [HttpPost]
    [EndpointSummary("Register User for Event Session")]
    [EndpointDescription("Create a new event registration (may require approval depending on registration mode)")]
    [Authorize]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventRegistrationDto dto, CancellationToken cancellationToken = default)
    {
        var command = new CreateEventRegistrationCommand { EventRegistrationDto = dto };
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    // PUT: api/eventregistration/{id}
    [HttpPut("{id}")]
    [EndpointSummary("Update Event Registration")]
    [EndpointDescription("Update an existing event registration (e.g., change approval status)")]
    [Authorize]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateEventRegistrationDto dto, CancellationToken cancellationToken = default)
    {
        if (id != dto.Id)
        {
            return Problem(detail: "Event Registration ID mismatch", statusCode: StatusCodes.Status400BadRequest, title: "Bad request");
        }

        var command = new UpdateEventRegistrationCommand { EventRegistrationDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    // DELETE: api/eventregistration/{id}
    [HttpDelete("{id}")]
    [EndpointSummary("Cancel Event Registration")]
    [EndpointDescription("Delete/cancel a user's event registration")]
    [Authorize]
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
}

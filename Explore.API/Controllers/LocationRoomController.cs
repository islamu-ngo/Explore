// ABOUTME: REST API controller for location room CRUD operations scoped to locations.
// ABOUTME: Manages rooms within locations for session venue assignment with HATEOAS.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Hateoas;
using Explore.Application.DTOs.LocationRoom;
using Explore.Application.Features.LocationRooms.Requests.Commands;
using Explore.Application.Features.LocationRooms.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

/// <summary>
/// Location Room management API endpoints.
/// Rooms represent bookable spaces within a location for session scheduling.
/// All responses include HATEOAS links by default.
/// Send "Prefer: return=minimal" header to strip links.
/// </summary>
[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public class LocationRoomController : ControllerBase
{
    private static readonly ApiValidationProblemDescriptor CreateValidationProblem = new(
        "locationRoom",
        "Location room validation failed",
        "Location room creation failed.");

    private static readonly ApiValidationProblemDescriptor UpdateValidationProblem = new(
        "locationRoom",
        "Location room validation failed",
        "Location room update failed.");

    private readonly IMediator _mediator;
    private readonly ILogger<LocationRoomController> _logger;
    private readonly IResourceAssembler<LocationRoomDto, LocationRoomListDto> _resourceAssembler;

    public LocationRoomController(
        IMediator mediator,
        ILogger<LocationRoomController> logger,
        IResourceAssembler<LocationRoomDto, LocationRoomListDto> resourceAssembler)
    {
        _mediator = mediator;
        _logger = logger;
        _resourceAssembler = resourceAssembler;
    }

    /// <summary>
    /// Get all rooms for a specific location.
    /// </summary>
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("by-location/{locationId:guid}", Name = RouteNames.GetLocationRoomsByLocation)]
    [EndpointSummary("Get Rooms by Location")]
    [EndpointDescription("Get all rooms for a specific location, ordered by sort order.")]
    [ProducesResponseType(typeof(HalCollectionResource<LocationRoomListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<HalCollectionResource<LocationRoomListDto>>> GetByLocation(Guid locationId, CancellationToken cancellationToken = default)
    {
        var rooms = await _mediator.Send(new GetLocationRoomsByLocationRequest { LocationId = locationId }, cancellationToken);

        var halResource = await _resourceAssembler.ToCollectionResource(
            rooms,
            RouteNames.GetLocationRoomsByLocation,
            new { locationId },
            HttpContext);

        return Ok(halResource);
    }

    /// <summary>
    /// Get location room details by ID.
    /// </summary>
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("{id:guid}", Name = RouteNames.GetLocationRoomById)]
    [EndpointSummary("Get Room Details")]
    [EndpointDescription("Get detailed information about a specific location room.")]
    [ProducesResponseType(typeof(HalResource<LocationRoomDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<HalResource<LocationRoomDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var room = await _mediator.Send(new GetLocationRoomDetailRequest { Id = id }, cancellationToken);
        if (room == null)
            return NotFound();

        var halResource = await _resourceAssembler.ToResource(room, HttpContext);
        return Ok(halResource);
    }

    /// <summary>
    /// Create a new location room.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost(Name = RouteNames.CreateLocationRoom)]
    [EndpointSummary("Create Room")]
    [EndpointDescription("Create a new room within a location.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateLocationRoomDto room, CancellationToken cancellationToken = default)
    {
        var command = new CreateLocationRoomCommand { LocationRoomDto = room };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return this.ToCommandValidationProblem(response, CreateValidationProblem);
        }

        return CreatedAtRoute(
            RouteNames.GetLocationRoomById,
            new { id = response.Id },
            response);
    }

    /// <summary>
    /// Update an existing location room.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPut("{id:guid}", Name = RouteNames.UpdateLocationRoom)]
    [EndpointSummary("Update Room")]
    [EndpointDescription("Update an existing location room.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateLocationRoomDto room, CancellationToken cancellationToken = default)
    {
        if (id != room.Id)
        {
            return this.ToValidationProblem(UpdateValidationProblem, "Room ID mismatch.");
        }

        var command = new UpdateLocationRoomCommand { LocationRoomDto = room };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return this.ToCommandValidationProblem(response, UpdateValidationProblem);
        }

        return Ok(response);
    }

    /// <summary>
    /// Delete a location room.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpDelete("{id:guid}", Name = RouteNames.DeleteLocationRoom)]
    [EndpointSummary("Delete Room")]
    [EndpointDescription("Delete a location room.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteLocationRoomCommand { Id = id };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }
}

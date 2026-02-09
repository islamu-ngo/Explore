using Explore.API.Hateoas;
using Explore.Application.DTOs.Location;
using Explore.Application.Features.Locations.Requests.Commands;
using Explore.Application.Features.Locations.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

/// <summary>
/// Location management API endpoints.
/// All responses include HATEOAS links by default.
/// Send "Prefer: return=minimal" header to strip links.
/// </summary>
[Route("api/v1/[controller]")]
[ApiController]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public class LocationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<LocationController> _logger;
    private readonly IResourceAssembler<LocationDto, LocationListDto> _resourceAssembler;

    public LocationController(
        IMediator mediator,
        ILogger<LocationController> logger,
        IResourceAssembler<LocationDto, LocationListDto> resourceAssembler)
    {
        _mediator = mediator;
        _logger = logger;
        _resourceAssembler = resourceAssembler;
    }

    /// <summary>
    /// Get all locations with pagination.
    /// </summary>
    [HttpGet(Name = RouteNames.GetLocations)]
    [EndpointSummary("Get all Locations")]
    [EndpointDescription("Retrieve a paginated list of all locations. " +
        "Default page size is 20, max is 100. " +
        "Response includes HATEOAS navigation links. " +
        "Send 'Prefer: return=minimal' header to strip links.")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HalCollectionResource<LocationListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<HalCollectionResource<LocationListDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetLocationListRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);

        var halResource = _resourceAssembler.ToCollectionResource(
            result,
            RouteNames.GetLocations,
            additionalRouteValues: null,
            HttpContext);

        return Ok(halResource);
    }

    /// <summary>
    /// Get location details by ID.
    /// </summary>
    [HttpGet("{id:guid}", Name = RouteNames.GetLocationById)]
    [EndpointSummary("Get Location Details")]
    [EndpointDescription("Get detailed information about a specific location including coordinates.")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HalResource<LocationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<HalResource<LocationDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var location = await _mediator.Send(new GetLocationDetailsRequest { Id = id }, cancellationToken);

        if (location is null)
        {
            return NotFound(new { error = "Location not found" });
        }

        var halResource = _resourceAssembler.ToResource(location, HttpContext);
        return Ok(halResource);
    }

    /// <summary>
    /// Get locations by city.
    /// </summary>
    [HttpGet("by-city/{city}", Name = RouteNames.GetLocationsByCity)]
    [EndpointSummary("Get Locations by City")]
    [EndpointDescription("Get all locations in a specific city.")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HalCollectionResource<LocationListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<HalCollectionResource<LocationListDto>>> GetByCity(string city, CancellationToken cancellationToken = default)
    {
        var locations = await _mediator.Send(new GetLocationsByCityRequest { City = city }, cancellationToken);

        var halResource = _resourceAssembler.ToCollectionResource(
            locations,
            RouteNames.GetLocationsByCity,
            HttpContext);

        return Ok(halResource);
    }

    /// <summary>
    /// Get locations by country.
    /// </summary>
    [HttpGet("by-country/{country}", Name = RouteNames.GetLocationsByCountry)]
    [EndpointSummary("Get Locations by Country")]
    [EndpointDescription("Get all locations in a specific country.")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HalCollectionResource<LocationListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<HalCollectionResource<LocationListDto>>> GetByCountry(string country, CancellationToken cancellationToken = default)
    {
        var locations = await _mediator.Send(new GetLocationsByCountryRequest { Country = country }, cancellationToken);

        var halResource = _resourceAssembler.ToCollectionResource(
            locations,
            RouteNames.GetLocationsByCountry,
            HttpContext);

        return Ok(halResource);
    }

    /// <summary>
    /// Create a new location.
    /// </summary>
    [HttpPost(Name = RouteNames.CreateLocation)]
    [EndpointSummary("Create Location")]
    [EndpointDescription("Create a new location with address and optional coordinates.")]
    [Authorize]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateLocationDto location, CancellationToken cancellationToken = default)
    {
        var command = new CreateLocationCommand { LocationDto = location };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtRoute(
            RouteNames.GetLocationById,
            new { id = response.Id },
            response);
    }

    /// <summary>
    /// Update an existing location.
    /// </summary>
    [HttpPut("{id:guid}", Name = RouteNames.UpdateLocation)]
    [EndpointSummary("Update Location")]
    [EndpointDescription("Update an existing location's information.")]
    [Authorize]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateLocationDto location, CancellationToken cancellationToken = default)
    {
        if (id != location.Id)
        {
            return BadRequest(new { error = "Location ID mismatch" });
        }

        var command = new UpdateLocationCommand { LocationDto = location };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Delete a location.
    /// </summary>
    [HttpDelete("{id:guid}", Name = RouteNames.DeleteLocation)]
    [EndpointSummary("Delete Location")]
    [EndpointDescription("Delete a location.")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new DeleteLocationCommand { Id = id };
            var result = await _mediator.Send(command, cancellationToken);

            if (!result)
            {
                return NotFound(new { error = "Location not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting location {LocationId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

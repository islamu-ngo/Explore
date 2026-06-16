// ABOUTME: REST API controller for venue/location CRUD operations with HATEOAS support.
// ABOUTME: Manages event venues, addresses, and geographic data for event discovery filtering.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Hateoas;
using Explore.API.Models;
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
[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public class LocationController : ControllerBase
{
    private static readonly ApiValidationProblemDescriptor CreateValidationProblem = new(
        "location",
        "Location validation failed",
        "Location creation failed.");

    private static readonly ApiValidationProblemDescriptor UpdateValidationProblem = new(
        "location",
        "Location validation failed",
        "Location update failed.");

    private static readonly ApiNotFoundProblemDescriptor LocationNotFoundProblem = new(
        "Location not found",
        "Location not found.");

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
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet(Name = RouteNames.GetLocations)]
    [EndpointSummary("Get all Locations")]
    [EndpointDescription("Retrieve a paginated list of all locations. " +
        "Default page size is 20, max is 100. " +
        "Response includes HATEOAS navigation links. " +
        "Send 'Prefer: return=minimal' header to strip links.")]
    [ProducesResponseType(typeof(HalCollectionResource<LocationListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<HalCollectionResource<LocationListDto>>> GetAll(
        [FromQuery] PaginationQueryRequest query,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetLocationListRequest
        {
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        }, cancellationToken);

        var halResource = await _resourceAssembler.ToCollectionResource(
            result,
            RouteNames.GetLocations,
            additionalRouteValues: null,
            HttpContext);

        return Ok(halResource);
    }

    /// <summary>
    /// Get location details by ID.
    /// </summary>
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("{id:guid}", Name = RouteNames.GetLocationById)]
    [EndpointSummary("Get Location Details")]
    [EndpointDescription("Get detailed information about a specific location including coordinates.")]
    [ProducesResponseType(typeof(HalResource<LocationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<HalResource<LocationDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var location = await _mediator.Send(new GetLocationDetailsRequest { Id = id }, cancellationToken);
        if (location == null)
        {
            return this.ToNotFoundProblem(LocationNotFoundProblem);
        }

        var halResource = await _resourceAssembler.ToResource(location, HttpContext);
        return Ok(halResource);
    }

    /// <summary>
    /// Get locations by city.
    /// </summary>
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("by-city/{city}", Name = RouteNames.GetLocationsByCity)]
    [EndpointSummary("Get Locations by City")]
    [EndpointDescription("Get all locations in a specific city.")]
    [ProducesResponseType(typeof(HalCollectionResource<LocationListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<HalCollectionResource<LocationListDto>>> GetByCity(string city, CancellationToken cancellationToken = default)
    {
        var locations = await _mediator.Send(new GetLocationsByCityRequest { City = city }, cancellationToken);

        var halResource = await _resourceAssembler.ToCollectionResource(
            locations,
            RouteNames.GetLocationsByCity,
            HttpContext);

        return Ok(halResource);
    }

    /// <summary>
    /// Get locations by country.
    /// </summary>
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("by-country/{country}", Name = RouteNames.GetLocationsByCountry)]
    [EndpointSummary("Get Locations by Country")]
    [EndpointDescription("Get all locations in a specific country.")]
    [ProducesResponseType(typeof(HalCollectionResource<LocationListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<HalCollectionResource<LocationListDto>>> GetByCountry(string country, CancellationToken cancellationToken = default)
    {
        var locations = await _mediator.Send(new GetLocationsByCountryRequest { Country = country }, cancellationToken);

        var halResource = await _resourceAssembler.ToCollectionResource(
            locations,
            RouteNames.GetLocationsByCountry,
            HttpContext);

        return Ok(halResource);
    }

    /// <summary>
    /// Create a new location.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost(Name = RouteNames.CreateLocation)]
    [EndpointSummary("Create Location")]
    [EndpointDescription("Create a new location with address and optional coordinates.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateLocationDto location, CancellationToken cancellationToken = default)
    {
        var command = new CreateLocationCommand { LocationDto = location };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return this.ToCommandValidationProblem(response, CreateValidationProblem);
        }

        return CreatedAtRoute(
            RouteNames.GetLocationById,
            new { id = response.Id },
            response);
    }

    /// <summary>
    /// Update an existing location.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPut("{id:guid}", Name = RouteNames.UpdateLocation)]
    [EndpointSummary("Update Location")]
    [EndpointDescription("Update an existing location's information.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateLocationDto location, CancellationToken cancellationToken = default)
    {
        if (id != location.Id)
        {
            return this.ToValidationProblem(UpdateValidationProblem, "Location ID mismatch.");
        }

        var command = new UpdateLocationCommand { LocationDto = location };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return this.ToCommandValidationProblem(response, UpdateValidationProblem);
        }

        return Ok(response);
    }

    /// <summary>
    /// Delete a location.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpDelete("{id:guid}", Name = RouteNames.DeleteLocation)]
    [EndpointSummary("Delete Location")]
    [EndpointDescription("Delete a location.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteLocationCommand { Id = id };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }
}

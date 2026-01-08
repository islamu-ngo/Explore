using Explore.Application.DTOs.Location;
using Explore.Application.Features.Locations.Requests.Commands;
using Explore.Application.Features.Locations.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class LocationController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<LocationController> _logger;

        public LocationController(IMediator mediator, ILogger<LocationController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // GET: api/v1/location
        [HttpGet]
        [EndpointSummary("Get all Locations")]
        [EndpointDescription("Get a list of all locations")]
        [AllowAnonymous]
        public async Task<ActionResult<List<LocationListDto>>> GetAll()
        {
            var locations = await _mediator.Send(new GetLocationListRequest());
            return Ok(locations);
        }

        // GET: api/v1/location/{id}
        [HttpGet("{id}")]
        [EndpointSummary("Get Location Details")]
        [EndpointDescription("Get detailed information about a specific location")]
        [AllowAnonymous]
        public async Task<ActionResult<LocationDto>> GetById(Guid id)
        {
            var location = await _mediator.Send(new GetLocationDetailsRequest { Id = id });

            if (location == null)
            {
                return NotFound(new { error = "Location not found" });
            }

            return Ok(location);
        }

        // GET: api/v1/location/by-city/{city}
        [HttpGet("by-city/{city}")]
        [EndpointSummary("Get Locations by City")]
        [EndpointDescription("Get all locations in a specific city")]
        [AllowAnonymous]
        public async Task<ActionResult<List<LocationListDto>>> GetByCity(string city)
        {
            var locations = await _mediator.Send(new GetLocationsByCityRequest { City = city });
            return Ok(locations);
        }

        // GET: api/v1/location/by-country/{country}
        [HttpGet("by-country/{country}")]
        [EndpointSummary("Get Locations by Country")]
        [EndpointDescription("Get all locations in a specific country")]
        [AllowAnonymous]
        public async Task<ActionResult<List<LocationListDto>>> GetByCountry(string country)
        {
            var locations = await _mediator.Send(new GetLocationsByCountryRequest { Country = country });
            return Ok(locations);
        }

        // POST: api/v1/location
        [HttpPost]
        [EndpointSummary("Create Location")]
        [EndpointDescription("Create a new location")]
        [Authorize]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateLocationDto location)
        {
            var command = new CreateLocationCommand { LocationDto = location };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        // PUT: api/v1/location/{id}
        [HttpPut("{id}")]
        [EndpointSummary("Update Location")]
        [EndpointDescription("Update an existing location")]
        [Authorize]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateLocationDto location)
        {
            if (id != location.Id)
            {
                return BadRequest(new { error = "Location ID mismatch" });
            }

            var command = new UpdateLocationCommand { LocationDto = location };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        // DELETE: api/v1/location/{id}
        [HttpDelete("{id}")]
        [EndpointSummary("Delete Location")]
        [EndpointDescription("Delete a location")]
        [Authorize]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                var command = new DeleteLocationCommand { Id = id };
                var result = await _mediator.Send(command);

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
}

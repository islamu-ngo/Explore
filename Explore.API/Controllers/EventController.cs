using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class EventController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<EventController> _logger;

        public EventController(IMediator mediator, IHttpContextAccessor httpContextAccessor, ILogger<EventController> logger)
        {
            _mediator = mediator;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // GET: api/<EventController>
        [HttpGet]
        [EndpointSummary("Get all Events (Conference, Webinar, Workshop ...)")]
        [EndpointDescription("Get a paginated list of all Events. Default page size is 20, max is 100.")]
        [AllowAnonymous]
        public async Task<ActionResult<PaginatedResult<EventListDto>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var events = await _mediator.Send(new GetEventListRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            });
            return Ok(events);
        }

        // GET: api/<EventController>/my
        [HttpGet("my")]
        [EndpointSummary("Get My Events")]
        [EndpointDescription("Get a paginated list of events created by the current user's organizations. Default page size is 20, max is 100.")]
        [Authorize]
        public async Task<ActionResult<PaginatedResult<EventListDto>>> GetMyEvents([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                _logger.LogInformation("=== GetMyEvents API Request ===");
                _logger.LogInformation($"User authenticated: {User?.Identity?.IsAuthenticated}");
                _logger.LogInformation($"User name: {User?.Identity?.Name}");

                // Log all claims for debugging
                var claims = _httpContextAccessor.HttpContext?.User?.Claims;
                if (claims != null)
                {
                    _logger.LogInformation("User Claims:");
                    foreach (var claim in claims)
                    {
                        _logger.LogInformation($"  {claim.Type}: {claim.Value}");
                    }
                }

                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
                    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
                    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sid")?.Value;

                _logger.LogInformation($"Extracted userId: {userId}");

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in token");
                    return Unauthorized(new { error = "User ID not found in token" });
                }

                _logger.LogInformation($"Sending GetMyEventsRequest for userId: {userId}");
                var events = await _mediator.Send(new GetMyEventsRequest
                {
                    UserId = userId,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                });

                _logger.LogInformation($"Retrieved {events?.Items?.Count ?? 0} events");
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMyEvents");
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // GET api/<EventController>/5
        [HttpGet("{id}")]
        [EndpointSummary("Get Event (Conference, Webinar, Workshop ...) Details")]
        [EndpointDescription("Get Details of the Event!")]
        [AllowAnonymous]
        public async Task<ActionResult<EventDto>> GetById(Guid id)
        {
            var @event = await _mediator.Send(new GetEventDetailsRequest{Id = id});
            return Ok(@event);
        }

        // POST api/<EventController>
        [HttpPost]
        [EndpointSummary("Create a new Event")]
        [EndpointDescription("Creates a new event. If OrganizationId is provided, the event is created under that organization (user must be admin). If null, the event is created under the user's personal actor.")]
        [Authorize]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventDto @event)
        {
            var command = new CreateEventCommand { EventDto = @event };
            var response = await _mediator.Send(command);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }
            
            return Ok(response);
        }

        // PUT api/<EventController>/5
        [HttpPut("{id}")]
        [EndpointSummary("Update an Event")]
        [EndpointDescription("Update an existing event")]
        [Authorize]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateEventDto @event)
        {
            if (id != @event.Id)
            {
                return BadRequest(new { error = "Event ID mismatch" });
            }

            var command = new UpdateEventCommand { EventDto = @event };
            var response = await _mediator.Send(command);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }
            
            return Ok(response);
        }

        // DELETE api/<EventController>/5
        [HttpDelete("{id}")]
        [EndpointSummary("Delete an Event")]
        [EndpointDescription("Delete an event (only if user owns the organization)")]
        [Authorize]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
                    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
                    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sid")?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "User ID not found in token" });
                }

                var command = new DeleteEventCommand { Id = id, UserId = userId };
                var result = await _mediator.Send(command);

                if (!result)
                {
                    return NotFound(new { error = "Event not found or you don't have permission to delete it" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event {EventId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}

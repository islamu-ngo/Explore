using Explore.Application.DTOs.EventSession;
using Explore.Application.Features.EventSessions.Requests.Commands;
using Explore.Application.Features.EventSessions.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class EventSessionController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<EventSessionController> _logger;

        public EventSessionController(IMediator mediator, ILogger<EventSessionController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // GET: api/v1/eventsession
        [HttpGet]
        [EndpointSummary("Get all Event Sessions")]
        [EndpointDescription("Get a paginated list of all event sessions. Default page size is 20, max is 100.")]
        [AllowAnonymous]
        public async Task<ActionResult<PaginatedResult<EventSessionListDto>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var sessions = await _mediator.Send(new GetEventSessionListRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            });
            return Ok(sessions);
        }

        // GET: api/v1/eventsession/{id}
        [HttpGet("{id}")]
        [EndpointSummary("Get Event Session Details")]
        [EndpointDescription("Get detailed information about a specific event session")]
        [AllowAnonymous]
        public async Task<ActionResult<EventSessionDto>> GetById(Guid id)
        {
            var session = await _mediator.Send(new GetEventSessionDetailsRequest { Id = id });

            if (session == null)
            {
                return NotFound(new { error = "Event session not found" });
            }

            return Ok(session);
        }

        // GET: api/v1/eventsession/by-event/{eventId}
        [HttpGet("by-event/{eventId}")]
        [EndpointSummary("Get Sessions by Event")]
        [EndpointDescription("Get all sessions for a specific event")]
        [AllowAnonymous]
        public async Task<ActionResult<List<EventSessionListDto>>> GetByEvent(Guid eventId)
        {
            var sessions = await _mediator.Send(new GetSessionsByEventRequest { EventId = eventId });
            return Ok(sessions);
        }

        // POST: api/v1/eventsession
        [HttpPost]
        [EndpointSummary("Create Event Session")]
        [EndpointDescription("Create a new event session")]
        [Authorize]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventSessionDto session)
        {
            var command = new CreateEventSessionCommand { EventSessionDto = session };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        // PUT: api/v1/eventsession/{id}
        [HttpPut("{id}")]
        [EndpointSummary("Update Event Session")]
        [EndpointDescription("Update an existing event session")]
        [Authorize]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateEventSessionDto session)
        {
            if (id != session.Id)
            {
                return BadRequest(new { error = "Event session ID mismatch" });
            }

            var command = new UpdateEventSessionCommand { EventSessionDto = session };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        // DELETE: api/v1/eventsession/{id}
        [HttpDelete("{id}")]
        [EndpointSummary("Delete Event Session")]
        [EndpointDescription("Delete an event session")]
        [Authorize]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                var command = new DeleteEventSessionCommand { Id = id };
                var result = await _mediator.Send(command);

                if (!result)
                {
                    return NotFound(new { error = "Event session not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event session {SessionId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Application.DTOs.EventSessionSpeaker;
using Explore.Application.Features.EventSessionSpeakers.Requests.Commands;
using Explore.Application.Features.EventSessionSpeakers.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class EventSessionSpeakerController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<EventSessionSpeakerController> _logger;

        public EventSessionSpeakerController(IMediator mediator, ILogger<EventSessionSpeakerController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // GET: api/v1/eventsessionspeaker
        [HttpGet]
        [EndpointSummary("Get all Session Speakers")]
        [EndpointDescription("Retrieve a paginated list of all event session speaker assignments. Default page size is 20, max is 100.")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PaginatedResult<EventSessionSpeakerListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResult<EventSessionSpeakerListDto>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var speakers = await _mediator.Send(new GetEventSessionSpeakerListRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            });
            return Ok(speakers);
        }

        // GET: api/v1/eventsessionspeaker/{id}
        [HttpGet("{id}")]
        [EndpointSummary("Get Speaker Assignment Details")]
        [EndpointDescription("Get detailed information about a specific speaker assignment")]
        [AllowAnonymous]
        public async Task<ActionResult<EventSessionSpeakerDto>> GetById(Guid id)
        {
            var speaker = await _mediator.Send(new GetEventSessionSpeakerDetailsRequest { Id = id });

            if (speaker == null)
            {
                return NotFound(new { error = "Speaker assignment not found" });
            }

            return Ok(speaker);
        }

        // GET: api/v1/eventsessionspeaker/by-session/{sessionId}
        [HttpGet("by-session/{sessionId}")]
        [EndpointSummary("Get Speakers by Session")]
        [EndpointDescription("Get all speakers for a specific event session")]
        [AllowAnonymous]
        public async Task<ActionResult<List<EventSessionSpeakerListDto>>> GetBySession(Guid sessionId)
        {
            var speakers = await _mediator.Send(new GetSpeakersBySessionRequest { EventSessionId = sessionId });
            return Ok(speakers);
        }

        // GET: api/v1/eventsessionspeaker/by-actor/{actorId}
        [HttpGet("by-actor/{actorId}")]
        [EndpointSummary("Get Sessions by Actor")]
        [EndpointDescription("Get all sessions where a specific actor is speaking")]
        [AllowAnonymous]
        public async Task<ActionResult<List<EventSessionSpeakerListDto>>> GetByActor(Guid actorId)
        {
            var speakers = await _mediator.Send(new GetSessionsByActorRequest { ActorId = actorId });
            return Ok(speakers);
        }

        // POST: api/v1/eventsessionspeaker
        [HttpPost]
        [EndpointSummary("Assign Speaker to Session")]
        [EndpointDescription("Assign a speaker (actor) to an event session")]
        [Authorize]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventSessionSpeakerDto speaker)
        {
            var command = new CreateEventSessionSpeakerCommand { SpeakerDto = speaker };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        // PUT: api/v1/eventsessionspeaker/{id}
        [HttpPut("{id}")]
        [EndpointSummary("Update Speaker Assignment")]
        [EndpointDescription("Update an existing speaker assignment")]
        [Authorize]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateEventSessionSpeakerDto speaker)
        {
            if (id != speaker.Id)
            {
                return BadRequest(new { error = "Speaker assignment ID mismatch" });
            }

            var command = new UpdateEventSessionSpeakerCommand { SpeakerDto = speaker };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        // DELETE: api/v1/eventsessionspeaker/{id}
        [HttpDelete("{id}")]
        [EndpointSummary("Remove Speaker from Session")]
        [EndpointDescription("Remove a speaker assignment from an event session")]
        [Authorize]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                var command = new DeleteEventSessionSpeakerCommand { Id = id };
                var result = await _mediator.Send(command);

                if (!result)
                {
                    return NotFound(new { error = "Speaker assignment not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing speaker assignment {SpeakerId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}

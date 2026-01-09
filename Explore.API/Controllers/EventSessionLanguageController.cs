using Explore.Application.DTOs.EventSessionLanguage;
using Explore.Application.Features.EventSessionLanguages.Requests.Commands;
using Explore.Application.Features.EventSessionLanguages.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class EventSessionLanguageController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<EventSessionLanguageController> _logger;

        public EventSessionLanguageController(IMediator mediator, ILogger<EventSessionLanguageController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // GET: api/v1/eventsessionlanguage
        [HttpGet]
        [EndpointSummary("Get all Session Languages")]
        [EndpointDescription("Get a list of all event session language assignments")]
        [AllowAnonymous]
        public async Task<ActionResult<List<EventSessionLanguageListDto>>> GetAll()
        {
            var sessionLanguages = await _mediator.Send(new GetEventSessionLanguageListRequest());
            return Ok(sessionLanguages);
        }

        // GET: api/v1/eventsessionlanguage/{id}
        [HttpGet("{id}")]
        [EndpointSummary("Get Session Language Details")]
        [EndpointDescription("Get detailed information about a specific session language assignment")]
        [AllowAnonymous]
        public async Task<ActionResult<EventSessionLanguageDto>> GetById(int id)
        {
            var sessionLanguage = await _mediator.Send(new GetEventSessionLanguageDetailsRequest { Id = id });

            if (sessionLanguage == null)
            {
                return NotFound(new { error = "Session language assignment not found" });
            }

            return Ok(sessionLanguage);
        }

        // GET: api/v1/eventsessionlanguage/by-session/{sessionId}
        [HttpGet("by-session/{sessionId}")]
        [EndpointSummary("Get Languages by Session")]
        [EndpointDescription("Get all languages for a specific event session")]
        [AllowAnonymous]
        public async Task<ActionResult<List<EventSessionLanguageListDto>>> GetBySession(Guid sessionId)
        {
            var sessionLanguages = await _mediator.Send(new GetLanguagesBySessionRequest { EventSessionId = sessionId });
            return Ok(sessionLanguages);
        }

        // POST: api/v1/eventsessionlanguage
        [HttpPost]
        [EndpointSummary("Assign Language to Session")]
        [EndpointDescription("Assign a language to an event session")]
        [Authorize]
        public async Task<ActionResult<BaseCommandResponse<int>>> Create([FromBody] CreateEventSessionLanguageDto sessionLanguage)
        {
            var command = new CreateEventSessionLanguageCommand { SessionLanguageDto = sessionLanguage };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        // PUT: api/v1/eventsessionlanguage/{id}
        [HttpPut("{id}")]
        [EndpointSummary("Update Session Language")]
        [EndpointDescription("Update an existing session language assignment")]
        [Authorize]
        public async Task<ActionResult<BaseCommandResponse<int>>> Update(int id, [FromBody] UpdateEventSessionLanguageDto sessionLanguage)
        {
            if (id != sessionLanguage.Id)
            {
                return BadRequest(new { error = "Session language ID mismatch" });
            }

            var command = new UpdateEventSessionLanguageCommand { SessionLanguageDto = sessionLanguage };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        // DELETE: api/v1/eventsessionlanguage/{id}
        [HttpDelete("{id}")]
        [EndpointSummary("Remove Language from Session")]
        [EndpointDescription("Remove a language assignment from an event session")]
        [Authorize]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var command = new DeleteEventSessionLanguageCommand { Id = id };
                var result = await _mediator.Send(command);

                if (!result)
                {
                    return NotFound(new { error = "Session language assignment not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing session language assignment {SessionLanguageId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}

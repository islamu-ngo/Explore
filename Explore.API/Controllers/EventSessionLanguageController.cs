using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        [EndpointDescription("Retrieve a paginated list of all event session language assignments. Default page size is 20, max is 100.")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PaginatedResult<EventSessionLanguageListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResult<EventSessionLanguageListDto>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var eventSessionLanguages = await _mediator.Send(new GetEventSessionLanguageListRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            });
            return Ok(eventSessionLanguages);
        }

        // GET: api/v1/eventsessionlanguage/{id}
        [HttpGet("{id}")]
        [EndpointSummary("Get Session Language Details")]
        [EndpointDescription("Get detailed information about a specific session language assignment")]
        [AllowAnonymous]
        public async Task<ActionResult<EventSessionLanguageDto>> GetById(int id)
        {
            var eventSessionLanguage = await _mediator.Send(new GetEventSessionLanguageDetailsRequest { Id = id });
            return Ok(eventSessionLanguage);
        }

        // GET: api/v1/eventsessionlanguage/by-session/{sessionId}
        [HttpGet("by-session/{sessionId}")]
        [EndpointSummary("Get Languages by Session")]
        [EndpointDescription("Get all languages for a specific event session")]
        [AllowAnonymous]
        public async Task<ActionResult<List<EventSessionLanguageListDto>>> GetBySession(Guid sessionId)
        {
            var eventSessionLanguages = await _mediator.Send(new GetLanguagesBySessionRequest { EventSessionId = sessionId });
            return Ok(eventSessionLanguages);
        }

        // POST: api/v1/eventsessionlanguage
        [HttpPost]
        [EndpointSummary("Assign Language to Session")]
        [EndpointDescription("Assign a language to an event session")]
        [Authorize]
        public async Task<ActionResult<BaseCommandResponse<int>>> Create([FromBody] CreateEventSessionLanguageDto dto)
        {
            var command = new CreateEventSessionLanguageCommand { EventSessionLanguageDto = dto };
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        // PUT: api/v1/eventsessionlanguage/{id}
        [HttpPut("{id}")]
        [EndpointSummary("Update Session Language")]
        [EndpointDescription("Update an existing session language assignment")]
        [Authorize]
        public async Task<ActionResult<BaseCommandResponse<int>>> Update(int id, [FromBody] UpdateEventSessionLanguageDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(new { error = "Event Session Language ID mismatch" });
            }

            var command = new UpdateEventSessionLanguageCommand { EventSessionLanguageDto = dto };
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
                    return NotFound(new { error = "Event Session Language not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Event Session Language {Id}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}

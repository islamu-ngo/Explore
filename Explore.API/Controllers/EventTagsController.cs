using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.EventTags;
using Explore.Application.DTOs.Tag;
using Explore.Application.Features.EventTags.Requests.Commands;
using Explore.Application.Features.EventTags.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class EventTagsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EventTagsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/v1/eventtags
        [HttpGet]
        [EndpointSummary("Get all Event Tags")]
        [EndpointDescription("Retrieve a list of all event-tag assignments")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<EventTagsListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<EventTagsListDto>>> GetAll()
        {
            var eventTags = await _mediator.Send(new GetEventTagsListRequest());
            return Ok(eventTags);
        }

        // GET: api/v1/eventtags/{id}
        [HttpGet("{id}")]
        [EndpointSummary("Get Event Tag by ID")]
        [EndpointDescription("Retrieve details of a specific event-tag assignment")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(EventTagsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EventTagsDto>> GetById(Guid id)
        {
            var eventTags = await _mediator.Send(new GetEventTagsDetailsRequest { Id = id });
            return Ok(eventTags);
        }

        // GET: api/v1/eventtags/by-event/{eventId}
        [HttpGet("by-event/{eventId}")]
        [EndpointSummary("Get Tags by Event")]
        [EndpointDescription("Retrieve all tags assigned to a specific event")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<TagListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<TagListDto>>> GetTagsByEvent(Guid eventId)
        {
            var tags = await _mediator.Send(new GetTagsByEventRequest { EventId = eventId });
            return Ok(tags);
        }

        // GET: api/v1/eventtags/by-tag/{tagId}
        [HttpGet("by-tag/{tagId}")]
        [EndpointSummary("Get Events by Tag")]
        [EndpointDescription("Retrieve all events that have a specific tag assigned")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<EventListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<EventListDto>>> GetEventsByTag(Guid tagId)
        {
            var events = await _mediator.Send(new GetEventsByTagRequest { TagId = tagId });
            return Ok(events);
        }

        // POST: api/v1/eventtags
        [HttpPost]
        [EndpointSummary("Assign Tag to Event")]
        [EndpointDescription("Create a new event-tag assignment for discovery and filtering")]
        [Authorize]
        [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventTagsDto dto)
        {
            var command = new CreateEventTagsCommand { EventTagsDto = dto };
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        // PUT: api/v1/eventtags/{id}
        [HttpPut("{id}")]
        [EndpointSummary("Update Event Tag Assignment")]
        [EndpointDescription("Update an existing event-tag assignment")]
        [Authorize]
        [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateEventTagsDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(new { error = "Event Tag ID mismatch" });
            }

            var command = new UpdateEventTagsCommand { EventTagsDto = dto };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        // DELETE: api/v1/eventtags/{id}
        [HttpDelete("{id}")]
        [EndpointSummary("Remove Tag from Event")]
        [EndpointDescription("Delete an event-tag assignment")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(Guid id)
        {
            var command = new DeleteEventTagsCommand { Id = id };
            var result = await _mediator.Send(command);

            if (!result)
            {
                return NotFound(new { error = "Event Tag not found" });
            }

            return NoContent();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Application.DTOs.Category;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.EventCategories;
using Explore.Application.Features.EventCategories.Requests.Commands;
using Explore.Application.Features.EventCategories.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class EventCategoriesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EventCategoriesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/v1/eventcategories
        [HttpGet]
        [EndpointSummary("Get all Event Categories")]
        [EndpointDescription("Retrieve a list of all event-category assignments")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<EventCategoriesListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<EventCategoriesListDto>>> GetAll()
        {
            var eventCategories = await _mediator.Send(new GetEventCategoriesListRequest());
            return Ok(eventCategories);
        }

        // GET: api/v1/eventcategories/{id}
        [HttpGet("{id}")]
        [EndpointSummary("Get Event Category by ID")]
        [EndpointDescription("Retrieve details of a specific event-category assignment")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(EventCategoriesDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EventCategoriesDto>> GetById(Guid id)
        {
            var eventCategories = await _mediator.Send(new GetEventCategoriesDetailsRequest { Id = id });
            return Ok(eventCategories);
        }

        // GET: api/v1/eventcategories/by-event/{eventId}
        [HttpGet("by-event/{eventId}")]
        [EndpointSummary("Get Categories by Event")]
        [EndpointDescription("Retrieve all categories assigned to a specific event for hierarchical filtering")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<CategoryListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CategoryListDto>>> GetCategoriesByEvent(Guid eventId)
        {
            var categories = await _mediator.Send(new GetCategoriesByEventRequest { EventId = eventId });
            return Ok(categories);
        }

        // GET: api/v1/eventcategories/by-category/{categoryId}
        [HttpGet("by-category/{categoryId}")]
        [EndpointSummary("Get Events by Category")]
        [EndpointDescription("Retrieve all events that belong to a specific category (Aqidah, Fiqh, Tafsir, etc.)")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<EventListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<EventListDto>>> GetEventsByCategory(Guid categoryId)
        {
            var events = await _mediator.Send(new GetEventsByCategoryRequest { CategoryId = categoryId });
            return Ok(events);
        }

        // POST: api/v1/eventcategories
        [HttpPost]
        [EndpointSummary("Assign Category to Event")]
        [EndpointDescription("Create a new event-category assignment for content classification")]
        [Authorize]
        [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventCategoriesDto dto)
        {
            var command = new CreateEventCategoriesCommand { EventCategoriesDto = dto };
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        // PUT: api/v1/eventcategories/{id}
        [HttpPut("{id}")]
        [EndpointSummary("Update Event Category Assignment")]
        [EndpointDescription("Update an existing event-category assignment")]
        [Authorize]
        [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateEventCategoriesDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(new { error = "Event Category ID mismatch" });
            }

            var command = new UpdateEventCategoriesCommand { EventCategoriesDto = dto };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        // DELETE: api/v1/eventcategories/{id}
        [HttpDelete("{id}")]
        [EndpointSummary("Remove Category from Event")]
        [EndpointDescription("Delete an event-category assignment")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(Guid id)
        {
            var command = new DeleteEventCategoriesCommand { Id = id };
            var result = await _mediator.Send(command);

            if (!result)
            {
                return NotFound(new { error = "Event Category not found" });
            }

            return NoContent();
        }
    }
}

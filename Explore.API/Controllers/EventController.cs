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
    [Route("api/[controller]")]
    [ApiController]
    public class EventController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EventController(IMediator mediator, IHttpContextAccessor httpContextAccessor)
        {
            _mediator = mediator;
            _httpContextAccessor = httpContextAccessor;
        }

        // GET: api/<EventController>
        [HttpGet]
        [EndpointSummary("Get all Events (Conference, Webinar, Workshop ...)")]
        [EndpointDescription("Get A List of all the Events (pagination!)")]
        [AllowAnonymous]
        public async Task<ActionResult<List<EventListDto>>> GetAll()
        {
            var events = await _mediator.Send(new GetEventListRequest());
            return Ok(events);
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
        [EndpointSummary("")]
        [EndpointDescription("")]
        [Authorize]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventDto @event)
        {
            var command = new CreateEventCommand { EventDto = @event };
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        // PUT api/<EventController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<EventController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}

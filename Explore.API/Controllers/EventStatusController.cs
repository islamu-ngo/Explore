using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Application.DTOs.EventStatus;
using Explore.Application.Features.EventStatuses.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class EventStatusController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EventStatusController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/v1/eventstatus
        [HttpGet]
        [EndpointSummary("Get all Event Statuses")]
        [EndpointDescription("Retrieve a list of all event lifecycle statuses (Draft, Published, Cancelled, Completed)")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<EventStatusListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<EventStatusListDto>>> GetAll()
        {
            var eventStatuses = await _mediator.Send(new GetEventStatusListRequest());
            return Ok(eventStatuses);
        }

        // GET: api/v1/eventstatus/{id}
        [HttpGet("{id}")]
        [EndpointSummary("Get Event Status by ID")]
        [EndpointDescription("Retrieve details of a specific event lifecycle status")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(EventStatusDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EventStatusDto>> GetById(int id)
        {
            var eventStatus = await _mediator.Send(new GetEventStatusDetailsRequest { Id = id });
            return Ok(eventStatus);
        }
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Application.DTOs.EventFormat;
using Explore.Application.Features.EventFormats.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class EventFormatController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EventFormatController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/v1/eventformat
        [HttpGet]
        [EndpointSummary("Get all Event Formats")]
        [EndpointDescription("Retrieve a list of all event delivery formats (In-person Local, Digital Online, Hybrid)")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<EventFormatListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<EventFormatListDto>>> GetAll()
        {
            var eventFormats = await _mediator.Send(new GetEventFormatListRequest());
            return Ok(eventFormats);
        }

        // GET: api/v1/eventformat/{id}
        [HttpGet("{id}")]
        [EndpointSummary("Get Event Format by ID")]
        [EndpointDescription("Retrieve details of a specific event delivery format")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(EventFormatDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EventFormatDto>> GetById(int id)
        {
            var eventFormat = await _mediator.Send(new GetEventFormatDetailsRequest { Id = id });
            return Ok(eventFormat);
        }
    }
}

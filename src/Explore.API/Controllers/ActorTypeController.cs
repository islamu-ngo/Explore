using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Application.DTOs.ActorType;
using Explore.Application.Features.ActorTypes.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ActorTypeController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ActorTypeController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/v1/actortype
        [HttpGet]
        [EndpointSummary("Get all Actor Types")]
        [EndpointDescription("Retrieve a list of all actor types (User, Organization, Service, Bot)")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<ActorTypeListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ActorTypeListDto>>> GetAll()
        {
            var actorTypes = await _mediator.Send(new GetActorTypeListRequest());
            return Ok(actorTypes);
        }

        // GET: api/v1/actortype/{id}
        [HttpGet("{id}")]
        [EndpointSummary("Get Actor Type by ID")]
        [EndpointDescription("Retrieve details of a specific actor type")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ActorTypeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ActorTypeDto>> GetById(int id)
        {
            var actorType = await _mediator.Send(new GetActorTypeDetailsRequest { Id = id });
            return Ok(actorType);
        }
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Application.DTOs.ActorKeyStore;
using Explore.Application.Features.ActorKeyStores.Requests.Commands;
using Explore.Application.Features.ActorKeyStores.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ActorKeyStoreController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ActorKeyStoreController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/v1/actorkeystore
        [HttpGet]
        [EndpointSummary("Get all Actor Key Stores")]
        [EndpointDescription("Retrieve a list of all actor key stores")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<ActorKeyStoreListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ActorKeyStoreListDto>>> GetAll()
        {
            var keyStores = await _mediator.Send(new GetActorKeyStoreListRequest());
            return Ok(keyStores);
        }

        // GET: api/v1/actorkeystore/{id}
        [HttpGet("{id}")]
        [EndpointSummary("Get Actor Key Store by ID")]
        [EndpointDescription("Retrieve details of a specific actor key store")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ActorKeyStoreDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ActorKeyStoreDto>> GetById(Guid id)
        {
            var keyStore = await _mediator.Send(new GetActorKeyStoreDetailsRequest { Id = id });
            if (keyStore == null)
            {
                return NotFound();
            }

            return Ok(keyStore);
        }

        // POST: api/v1/actorkeystore
        [HttpPost]
        [EndpointSummary("Create new Actor Key Store")]
        [EndpointDescription("Create a new actor key store")]
        [Authorize]
        [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateActorKeyStoreDto dto)
        {
            var command = new CreateActorKeyStoreCommand { ActorKeyStoreDto = dto };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        // PUT: api/v1/actorkeystore/{id}
        [HttpPut("{id}")]
        [EndpointSummary("Update Actor Key Store")]
        [EndpointDescription("Update an existing actor key store")]
        [Authorize]
        [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateActorKeyStoreDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(new { error = "Actor Key Store ID mismatch" });
            }

            var command = new UpdateActorKeyStoreCommand { ActorKeyStoreDto = dto };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        // DELETE: api/v1/actorkeystore/{id}
        [HttpDelete("{id}")]
        [EndpointSummary("Delete Actor Key Store")]
        [EndpointDescription("Delete an actor key store")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(Guid id)
        {
            var command = new DeleteActorKeyStoreCommand { Id = id };
            var result = await _mediator.Send(command);

            if (!result)
            {
                return NotFound(new { error = "Actor Key Store not found" });
            }

            return NoContent();
        }
    }
}

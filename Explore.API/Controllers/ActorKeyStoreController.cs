// ABOUTME: API controller for managing actor cryptographic key storage and federation identity.
// ABOUTME: Handles DID key management, key rotation, and federation credential operations.

using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using Explore.Application.DTOs.ActorKeyStore;
using Explore.Application.Features.ActorKeyStores.Requests.Commands;
using Explore.Application.Features.ActorKeyStores.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
public class ActorKeyStoreController : ControllerBase
{
    private readonly IMediator _mediator;

    public ActorKeyStoreController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/actorkeystore
    [HttpGet]
    [EndpointSummary("Get all Actor Key Stores")]
    [EndpointDescription("Retrieve a list of all actor key stores")]
    [Authorize]
    [ProducesResponseType(typeof(List<ActorKeyStoreListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<List<ActorKeyStoreListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var keyStores = await _mediator.Send(new GetActorKeyStoreListRequest(), cancellationToken);
        return Ok(keyStores);
    }

    // GET: api/actorkeystore/{id}
    [HttpGet("{id}")]
    [EndpointSummary("Get Actor Key Store by ID")]
    [EndpointDescription("Retrieve details of a specific actor key store")]
    [Authorize]
    [ProducesResponseType(typeof(ActorKeyStoreDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<ActorKeyStoreDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var keyStore = await _mediator.Send(new GetActorKeyStoreDetailsRequest { Id = id }, cancellationToken);

        return Ok(keyStore);
    }

    // POST: api/actorkeystore
    [HttpPost]
    [EndpointSummary("Create new Actor Key Store")]
    [EndpointDescription("Create a new actor key store")]
    [Authorize]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateActorKeyStoreDto dto, CancellationToken cancellationToken = default)
    {
        var command = new CreateActorKeyStoreCommand { ActorKeyStoreDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    // PUT: api/actorkeystore/{id}
    [HttpPut("{id}")]
    [EndpointSummary("Update Actor Key Store")]
    [EndpointDescription("Update an existing actor key store")]
    [Authorize]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateActorKeyStoreDto dto, CancellationToken cancellationToken = default)
    {
        if (id != dto.Id)
        {
            return Problem(detail: "Actor Key Store ID mismatch", statusCode: StatusCodes.Status400BadRequest, title: "Bad request");
        }

        var command = new UpdateActorKeyStoreCommand { ActorKeyStoreDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    // DELETE: api/actorkeystore/{id}
    [HttpDelete("{id}")]
    [EndpointSummary("Delete Actor Key Store")]
    [EndpointDescription("Delete an actor key store")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteActorKeyStoreCommand { Id = id };
        var result = await _mediator.Send(command, cancellationToken);

        if (!result)
        {
            return Problem(detail: "Actor Key Store not found", statusCode: StatusCodes.Status404NotFound, title: "Resource not found");
        }

        return NoContent();
    }
}

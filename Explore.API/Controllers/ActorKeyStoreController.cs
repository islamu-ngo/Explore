// ABOUTME: API controller for managing actor cryptographic key storage and federation identity.
// ABOUTME: Handles DID key management, key rotation, and federation credential operations.

using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Hateoas;
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
[EndpointClassification(EndpointClass.Authenticated)]
public class ActorKeyStoreController : ControllerBase
{
    private static readonly ApiValidationProblemDescriptor CreateValidationProblem = new(
        "actorKeyStore",
        "Actor key store validation failed",
        "Actor key store creation failed.");

    private static readonly ApiValidationProblemDescriptor UpdateValidationProblem = new(
        "actorKeyStore",
        "Actor key store validation failed",
        "Actor key store update failed.");

    private static readonly ApiNotFoundProblemDescriptor KeyStoreNotFoundProblem = new(
        "Actor key store not found",
        "Actor key store not found.");

    private readonly IMediator _mediator;

    public ActorKeyStoreController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/actorkeystore
    [HttpGet(Name = RouteNames.GetActorKeyStores)]
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
    [HttpGet("{id}", Name = RouteNames.GetActorKeyStoreById)]
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
    [HttpPost(Name = RouteNames.CreateActorKeyStore)]
    [EndpointSummary("Create new Actor Key Store")]
    [EndpointDescription("Create a new actor key store")]
    [Authorize]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateActorKeyStoreDto dto, CancellationToken cancellationToken = default)
    {
        var command = new CreateActorKeyStoreCommand { ActorKeyStoreDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return this.ToCommandValidationProblem(response, CreateValidationProblem);
        }

        return Ok(response);
    }

    // PUT: api/actorkeystore/{id}
    [HttpPut("{id}", Name = RouteNames.UpdateActorKeyStore)]
    [EndpointSummary("Update Actor Key Store")]
    [EndpointDescription("Update an existing actor key store")]
    [Authorize]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateActorKeyStoreDto dto, CancellationToken cancellationToken = default)
    {
        if (id != dto.Id)
        {
            return this.ToValidationProblem(UpdateValidationProblem, "Actor key store ID mismatch.");
        }

        var command = new UpdateActorKeyStoreCommand { ActorKeyStoreDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return this.ToCommandValidationProblem(response, UpdateValidationProblem);
        }

        return Ok(response);
    }

    // DELETE: api/actorkeystore/{id}
    [HttpDelete("{id}", Name = RouteNames.DeleteActorKeyStore)]
    [EndpointSummary("Delete Actor Key Store")]
    [EndpointDescription("Delete an actor key store")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteActorKeyStoreCommand { Id = id };
        var result = await _mediator.Send(command, cancellationToken);

        if (!result)
        {
            return this.ToNotFoundProblem(KeyStoreNotFoundProblem);
        }

        return NoContent();
    }
}

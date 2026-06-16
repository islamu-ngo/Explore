// ABOUTME: API controller for managing sync state records used in federation and data synchronization.
// ABOUTME: Tracks external system synchronization status and provides endpoints for sync operations.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Hateoas;
using Explore.Application.DTOs.SyncState;
using Explore.Application.Features.SyncStates.Requests.Commands;
using Explore.Application.Features.SyncStates.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[EndpointClassification(EndpointClass.Authenticated)]
public class SyncStateController : ControllerBase
{
    private static readonly ApiValidationProblemDescriptor UpdateValidationProblem = new(
        "syncState",
        "SyncState validation failed",
        "SyncState update failed.");

    private readonly IMediator _mediator;
    private readonly ILogger<SyncStateController> _logger;

    public SyncStateController(
        IMediator mediator,
        ILogger<SyncStateController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // GET: api/syncstate
    [HttpGet(Name = RouteNames.GetSyncStates)]
    [Authorize]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<List<SyncStateListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var syncStates = await _mediator.Send(new GetSyncStateListRequest(), cancellationToken);
        return Ok(syncStates);
    }

    // GET: api/syncstate/{id}
    [HttpGet("{id}", Name = RouteNames.GetSyncStateById)]
    [Authorize]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<SyncStateDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var syncState = await _mediator.Send(new GetSyncStateDetailsRequest { Id = id }, cancellationToken);

        return Ok(syncState);
    }

    // POST: api/syncstate
    [HttpPost(Name = RouteNames.CreateSyncState)]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<int>>> Create([FromBody] CreateSyncStateDto dto, CancellationToken cancellationToken = default)
    {
        var command = new CreateSyncStateCommand { SyncStateDto = dto };
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    // PUT: api/syncstate/{id}
    [HttpPut("{id}", Name = RouteNames.UpdateSyncState)]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<int>>> Update(int id, [FromBody] UpdateSyncStateDto dto, CancellationToken cancellationToken = default)
    {
        if (id != dto.Id)
        {
            return this.ToValidationProblem(UpdateValidationProblem, "SyncState ID mismatch.");
        }

        var command = new UpdateSyncStateCommand { SyncStateDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return this.ToCommandValidationProblem(response, UpdateValidationProblem);
        }

        return Ok(response);
    }

    // DELETE: api/syncstate/{id}
    [HttpDelete("{id}", Name = RouteNames.DeleteSyncState)]
    [Authorize]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteSyncStateCommand { Id = id };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }
}

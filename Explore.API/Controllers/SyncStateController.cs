using Asp.Versioning;
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
public class SyncStateController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SyncStateController> _logger;

    public SyncStateController(
        IMediator mediator,
        ILogger<SyncStateController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // GET: api/v1/syncstate
    [HttpGet]
    [Authorize]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<List<SyncStateListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var syncStates = await _mediator.Send(new GetSyncStateListRequest(), cancellationToken);
        return Ok(syncStates);
    }

    // GET: api/v1/syncstate/{id}
    [HttpGet("{id}")]
    [Authorize]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<SyncStateDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var syncState = await _mediator.Send(new GetSyncStateDetailsRequest { Id = id }, cancellationToken);
        if (syncState == null)
        {
            return NotFound(new { error = "SyncState not found" });
        }

        return Ok(syncState);
    }

    // POST: api/v1/syncstate
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<int>>> Create([FromBody] CreateSyncStateDto dto, CancellationToken cancellationToken = default)
    {
        var command = new CreateSyncStateCommand { SyncStateDto = dto };
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    // PUT: api/v1/syncstate/{id}
    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<int>>> Update(int id, [FromBody] UpdateSyncStateDto dto, CancellationToken cancellationToken = default)
    {
        if (id != dto.Id)
        {
            return BadRequest(new { error = "SyncState ID mismatch" });
        }

        var command = new UpdateSyncStateCommand { SyncStateDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    // DELETE: api/v1/syncstate/{id}
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteSyncStateCommand { Id = id };
        var result = await _mediator.Send(command, cancellationToken);

        if (!result)
        {
            return NotFound(new { error = "SyncState not found or you don't have permission to delete it" });
        }

        return NoContent();
    }
}

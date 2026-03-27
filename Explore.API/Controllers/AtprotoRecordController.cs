// ABOUTME: API controller for ATProto record management and federation operations.
// ABOUTME: Handles creation, updates, and deletion of ATProto records for federation support.

using Asp.Versioning;
using Explore.Application.DTOs.AtprotoRecord;
using Explore.Application.Features.AtprotoRecords.Requests.Commands;
using Explore.Application.Features.AtprotoRecords.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
public class AtprotoRecordController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AtprotoRecordController> _logger;

    public AtprotoRecordController(
        IMediator mediator,
        ILogger<AtprotoRecordController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // GET: api/atprotoRecord
    [HttpGet]
    [Authorize]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<List<AtprotoRecordListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var atprotoRecords = await _mediator.Send(new GetAtprotoRecordListRequest(), cancellationToken);
        return Ok(atprotoRecords);
    }

    // GET: api/atprotoRecord/{id}
    [HttpGet("{id}")]
    [Authorize]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<AtprotoRecordDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var atprotoRecord = await _mediator.Send(new GetAtprotoRecordDetailsRequest { Id = id }, cancellationToken);

        return Ok(atprotoRecord);
    }

    // POST: api/atprotoRecord
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateAtprotoRecordDto dto, CancellationToken cancellationToken = default)
    {
        var command = new CreateAtprotoRecordCommand { AtprotoRecordDto = dto };
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    // PUT: api/atprotoRecord/{id}
    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateAtprotoRecordDto dto, CancellationToken cancellationToken = default)
    {
        if (id != dto.Id)
        {
            return BadRequest(new { error = "AtprotoRecord ID mismatch" });
        }

        var command = new UpdateAtprotoRecordCommand { AtprotoRecordDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    // DELETE: api/atprotoRecord/{id}
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteAtprotoRecordCommand { Id = id };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }
}

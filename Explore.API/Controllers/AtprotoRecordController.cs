using Explore.Application.DTOs.AtprotoRecord;
using Explore.Application.Features.AtprotoRecords.Requests.Commands;
using Explore.Application.Features.AtprotoRecords.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[Route("api/v1/[controller]")]
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

    // GET: api/v1/atprotoRecord
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<List<AtprotoRecordListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var atprotoRecords = await _mediator.Send(new GetAtprotoRecordListRequest(), cancellationToken);
        return Ok(atprotoRecords);
    }

    // GET: api/v1/atprotoRecord/{id}
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<AtprotoRecordDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var atprotoRecord = await _mediator.Send(new GetAtprotoRecordDetailsRequest { Id = id }, cancellationToken);
        if (atprotoRecord == null)
        {
            return NotFound(new { error = "AtprotoRecord not found" });
        }

        return Ok(atprotoRecord);
    }

    // POST: api/v1/atprotoRecord
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateAtprotoRecordDto dto, CancellationToken cancellationToken = default)
    {
        var command = new CreateAtprotoRecordCommand { AtprotoRecordDto = dto };
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    // PUT: api/v1/atprotoRecord/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
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

    // DELETE: api/v1/atprotoRecord/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteAtprotoRecordCommand { Id = id };
        var result = await _mediator.Send(command, cancellationToken);

        if (!result)
        {
            return NotFound(new { error = "AtprotoRecord not found or you don't have permission to delete it" });
        }

        return NoContent();
    }
}

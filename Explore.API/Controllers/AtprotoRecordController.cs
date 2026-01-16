using MediatR;
using Explore.Application.Features.AtprotoRecords.Requests.Commands;
using Explore.Application.Features.AtprotoRecords.Requests.Queries;
using Explore.Application.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Explore.Application.DTOs.AtprotoRecord;

namespace Explore.API.Controllers
{
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
        [AllowAnonymous]
        public async Task<ActionResult<List<AtprotoRecordListDto>>> GetAll()
        {
            var atprotoRecords = await _mediator.Send(new GetAtprotoRecordListRequest());
            return Ok(atprotoRecords);
        }

        // GET: api/v1/atprotoRecord/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<AtprotoRecordDto>> GetById(Guid id)
        {
            var atprotoRecord = await _mediator.Send(new GetAtprotoRecordDetailsRequest { Id = id });
            if (atprotoRecord == null)
            {
                return NotFound(new { error = "AtprotoRecord not found" });
            }

            return Ok(atprotoRecord);
        }

        // POST: api/v1/atprotoRecord
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateAtprotoRecordDto dto)
        {
            var command = new CreateAtprotoRecordCommand { AtprotoRecordDto = dto };
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        // PUT: api/v1/atprotoRecord/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateAtprotoRecordDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(new { error = "AtprotoRecord ID mismatch" });
            }

            var command = new UpdateAtprotoRecordCommand { AtprotoRecordDto = dto };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        // DELETE: api/v1/atprotoRecord/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> Delete(Guid id)
        {
            var command = new DeleteAtprotoRecordCommand { Id = id };
            var result = await _mediator.Send(command);

            if (!result)
            {
                return NotFound(new { error = "AtprotoRecord not found or you don't have permission to delete it" });
            }

            return NoContent();
        }
    }
}

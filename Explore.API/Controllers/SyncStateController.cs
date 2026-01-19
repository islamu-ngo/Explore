using MediatR;
using Explore.Application.Features.SyncStates.Requests.Commands;
using Explore.Application.Features.SyncStates.Requests.Queries;
using Explore.Application.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Explore.Application.DTOs.SyncState;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
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
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<SyncStateListDto>>> GetAll()
        {
            var syncStates = await _mediator.Send(new GetSyncStateListRequest());
            return Ok(syncStates);
        }

        // GET: api/v1/syncstate/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<SyncStateDto>> GetById(int id)
        {
            var syncState = await _mediator.Send(new GetSyncStateDetailsRequest { Id = id });
            if (syncState == null)
            {
                return NotFound(new { error = "SyncState not found" });
            }

            return Ok(syncState);
        }

        // POST: api/v1/syncstate
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<BaseCommandResponse<int>>> Create([FromBody] CreateSyncStateDto dto)
        {
            var command = new CreateSyncStateCommand { SyncStateDto = dto };
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        // PUT: api/v1/syncstate/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<BaseCommandResponse<int>>> Update(int id, [FromBody] UpdateSyncStateDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(new { error = "SyncState ID mismatch" });
            }

            var command = new UpdateSyncStateCommand { SyncStateDto = dto };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        // DELETE: api/v1/syncstate/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(int id)
        {
            var command = new DeleteSyncStateCommand { Id = id };
            var result = await _mediator.Send(command);

            if (!result)
            {
                return NotFound(new { error = "SyncState not found or you don't have permission to delete it" });
            }

            return NoContent();
        }
    }
}

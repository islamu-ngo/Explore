using MediatR;
using Explore.Application.Features.IndexedDids.Requests.Commands;
using Explore.Application.Features.IndexedDids.Requests.Queries;
using Explore.Application.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Explore.Application.DTOs.IndexedDid;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class IndexedDidController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<IndexedDidController> _logger;

        public IndexedDidController(
            IMediator mediator,
            ILogger<IndexedDidController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // GET: api/v1/indexeddid
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<IndexedDidListDto>>> GetAll()
        {
            var indexedDids = await _mediator.Send(new GetIndexedDidListRequest());
            return Ok(indexedDids);
        }

        // GET: api/v1/indexeddid/{did}
        [HttpGet("{did}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IndexedDidDto>> GetById(string did)
        {
            var indexedDid = await _mediator.Send(new GetIndexedDidDetailsRequest { Did = did });
            if (indexedDid == null)
            {
                return NotFound(new { error = "IndexedDid not found" });
            }

            return Ok(indexedDid);
        }

        // POST: api/v1/indexeddid
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<BaseCommandResponse<string>>> Create([FromBody] CreateIndexedDidDto dto)
        {
            var command = new CreateIndexedDidCommand { IndexedDidDto = dto };
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        // PUT: api/v1/indexeddid/{did}
        [HttpPut("{did}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<BaseCommandResponse<string>>> Update(string did, [FromBody] UpdateIndexedDidDto dto)
        {
            if (did != dto.Did)
            {
                return BadRequest(new { error = "IndexedDid DID mismatch" });
            }

            var command = new UpdateIndexedDidCommand { IndexedDidDto = dto };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        // DELETE: api/v1/indexeddid/{did}
        [HttpDelete("{did}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(string did)
        {
            var command = new DeleteIndexedDidCommand { Did = did };
            var result = await _mediator.Send(command);

            if (!result)
            {
                return NotFound(new { error = "IndexedDid not found or you don't have permission to delete it" });
            }

            return NoContent();
        }
    }
}

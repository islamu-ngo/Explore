using MediatR;
using Explore.Application.Features.IndexedDids.Requests.Commands;
using Explore.Application.Features.IndexedDids.Requests.Queries;
using Explore.Application.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Explore.Application.DTOs.IndexedDid;
using Explore.API.Hateoas;
using Explore.Application.Hateoas;

namespace Explore.API.Controllers
{
    [Route("api/v1/indexeddid")]
    [ApiController]
    [Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
    public class IndexedDidController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<IndexedDidController> _logger;
        private readonly IResourceAssembler<IndexedDidDto, IndexedDidListDto> _resourceAssembler;

        public IndexedDidController(
            IMediator mediator,
            ILogger<IndexedDidController> logger,
            IResourceAssembler<IndexedDidDto, IndexedDidListDto> resourceAssembler)
        {
            _mediator = mediator;
            _logger = logger;
            _resourceAssembler = resourceAssembler;
        }

        // GET: api/v1/indexeddid
        [HttpGet(Name = RouteNames.GetIndexedDids)]
        [AllowAnonymous]
        public async Task<ActionResult<HalCollectionResource<IndexedDidListDto>>> GetAll()
        {
            var indexedDids = await _mediator.Send(new GetIndexedDidListRequest());
            var halResource = _resourceAssembler.ToCollectionResource(
                indexedDids,
                RouteNames.GetIndexedDids,
                HttpContext);
            return Ok(halResource);
        }

        // GET: api/v1/indexeddid/{did}
        [HttpGet("{did}", Name = RouteNames.GetIndexedDidByDid)]
        [AllowAnonymous]
        public async Task<ActionResult<HalResource<IndexedDidDto>>> GetById(string did)
        {
            var indexedDid = await _mediator.Send(new GetIndexedDidDetailsRequest { Did = did });
            if (indexedDid == null)
            {
                return NotFound(new { error = "IndexedDid not found" });
            }

            var halResource = _resourceAssembler.ToResource(indexedDid, HttpContext);
            return Ok(halResource);
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

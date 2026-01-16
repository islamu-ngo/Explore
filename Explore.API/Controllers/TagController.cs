using Explore.Application.DTOs.Tag;
using Explore.Application.Features.Tags.Requests.Commands;
using Explore.Application.Features.Tags.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class TagController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<TagController> _logger;

        public TagController(IMediator mediator, ILogger<TagController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // GET: api/v1/tag
        [HttpGet]
        [EndpointSummary("Get all Tags")]
        [EndpointDescription("Retrieve a paginated list of all tags for events. Default page size is 20, max is 100.")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PaginatedResult<TagListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResult<TagListDto>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var tags = await _mediator.Send(new GetTagListRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            });
            return Ok(tags);
        }

        // GET: api/v1/tag/{id}
        [HttpGet("{id}")]
        [EndpointSummary("Get Tag Details")]
        [EndpointDescription("Get detailed information about a specific tag")]
        [AllowAnonymous]
        public async Task<ActionResult<TagDto>> GetById(Guid id)
        {
            var tag = await _mediator.Send(new GetTagDetailsRequest { Id = id });

            if (tag == null)
            {
                return NotFound(new { error = "Tag not found" });
            }

            return Ok(tag);
        }

        // POST: api/v1/tag
        [HttpPost]
        [EndpointSummary("Create Tag")]
        [EndpointDescription("Create a new tag for events")]
        [Authorize]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateTagDto tag)
        {
            var command = new CreateTagCommand { TagDto = tag };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        // PUT: api/v1/tag/{id}
        [HttpPut("{id}")]
        [EndpointSummary("Update Tag")]
        [EndpointDescription("Update an existing tag")]
        [Authorize]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateTagDto tag)
        {
            if (id != tag.Id)
            {
                return BadRequest(new { error = "Tag ID mismatch" });
            }

            var command = new UpdateTagCommand { TagDto = tag };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        // DELETE: api/v1/tag/{id}
        [HttpDelete("{id}")]
        [EndpointSummary("Delete Tag")]
        [EndpointDescription("Delete a tag")]
        [Authorize]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                var command = new DeleteTagCommand { Id = id };
                var result = await _mediator.Send(command);

                if (!result)
                {
                    return NotFound(new { error = "Tag not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tag {TagId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}

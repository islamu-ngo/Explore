using Explore.Application.DTOs.Tag;
using Explore.Application.DTOs.TagType;
using Explore.Application.DTOs.TagTypeTags;
using Explore.Application.Features.TagTypeTags.Requests.Commands;
using Explore.Application.Features.TagTypeTags.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class TagTypeTagsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<TagTypeTagsController> _logger;

        public TagTypeTagsController(IMediator mediator, ILogger<TagTypeTagsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // GET: api/v1/tagtypetags
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<TagTypeTagsListDto>>> GetAll()
        {
            var tagTypeTags = await _mediator.Send(new GetTagTypeTagsListRequest());
            return Ok(tagTypeTags);
        }

        // GET: api/v1/tagtypetags/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<TagTypeTagsDto>> GetById(Guid id)
        {
            var tagTypeTags = await _mediator.Send(new GetTagTypeTagsDetailsRequest { Id = id });
            return Ok(tagTypeTags);
        }

        // GET: api/v1/tagtypetags/by-tagtype/{tagTypeId}
        [HttpGet("by-tagtype/{tagTypeId}")]
        [AllowAnonymous]
        public async Task<ActionResult<List<TagListDto>>> GetTagsByTagType(int tagTypeId)
        {
            var tags = await _mediator.Send(new GetTagsByTagTypeRequest { TagTypeId = tagTypeId });
            return Ok(tags);
        }

        // GET: api/v1/tagtypetags/by-tag/{tagId}
        [HttpGet("by-tag/{tagId}")]
        [AllowAnonymous]
        public async Task<ActionResult<List<TagTypeListDto>>> GetTagTypesForTag(Guid tagId)
        {
            var tagTypes = await _mediator.Send(new GetTagTypesForTagRequest { TagId = tagId });
            return Ok(tagTypes);
        }

        // POST: api/v1/tagtypetags
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateTagTypeTagsDto dto)
        {
            var command = new CreateTagTypeTagsCommand { TagTypeTagsDto = dto };
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        // PUT: api/v1/tagtypetags/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateTagTypeTagsDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(new { error = "Tag Type Tags ID mismatch" });
            }

            var command = new UpdateTagTypeTagsCommand { TagTypeTagsDto = dto };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        // DELETE: api/v1/tagtypetags/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                var command = new DeleteTagTypeTagsCommand { Id = id };
                var result = await _mediator.Send(command);

                if (!result)
                {
                    return NotFound(new { error = "Tag Type Tags not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Tag Type Tags {Id}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}

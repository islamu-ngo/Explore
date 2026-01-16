using Explore.Application.DTOs.Category;
using Explore.Application.Features.Categories.Requests.Commands;
using Explore.Application.Features.Categories.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(IMediator mediator, ILogger<CategoryController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // GET: api/v1/category
        [HttpGet]
        [EndpointSummary("Get all Categories")]
        [EndpointDescription("Retrieve a paginated list of all event categories. Default page size is 20, max is 100.")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PaginatedResult<CategoryListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResult<CategoryListDto>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var categories = await _mediator.Send(new GetCategoryListRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            });
            return Ok(categories);
        }

        // GET: api/v1/category/{id}
        [HttpGet("{id}")]
        [EndpointSummary("Get Category Details")]
        [EndpointDescription("Get detailed information about a specific category")]
        [AllowAnonymous]
        public async Task<ActionResult<CategoryDto>> GetById(Guid id)
        {
            var category = await _mediator.Send(new GetCategoryDetailsRequest { Id = id });

            if (category == null)
            {
                return NotFound(new { error = "Category not found" });
            }

            return Ok(category);
        }

        // POST: api/v1/category
        [HttpPost]
        [EndpointSummary("Create Category")]
        [EndpointDescription("Create a new event category")]
        [Authorize]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateCategoryDto category)
        {
            var command = new CreateCategoryCommand { CategoryDto = category };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        // PUT: api/v1/category/{id}
        [HttpPut("{id}")]
        [EndpointSummary("Update Category")]
        [EndpointDescription("Update an existing category")]
        [Authorize]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateCategoryDto category)
        {
            if (id != category.Id)
            {
                return BadRequest(new { error = "Category ID mismatch" });
            }

            var command = new UpdateCategoryCommand { CategoryDto = category };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        // DELETE: api/v1/category/{id}
        [HttpDelete("{id}")]
        [EndpointSummary("Delete Category")]
        [EndpointDescription("Delete a category")]
        [Authorize]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                var command = new DeleteCategoryCommand { Id = id };
                var result = await _mediator.Send(command);

                if (!result)
                {
                    return NotFound(new { error = "Category not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category {CategoryId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}

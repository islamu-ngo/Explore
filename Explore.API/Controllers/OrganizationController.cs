using Explore.Application.DTOs.Organization;
using Explore.Application.Features.Organizations.Requests.Commands;
using Explore.Application.Features.Organizations.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class OrganizationController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OrganizationController(IMediator mediator, IHttpContextAccessor httpContextAccessor)
        {
            _mediator = mediator;
            _httpContextAccessor = httpContextAccessor;
        }

        // GET: api/<OrganizationController>
        [HttpGet]
        [EndpointSummary("Get all Organizations")]
        [EndpointDescription("Get a paginated list of all Organizations. Default page size is 20, max is 100.")]
        [AllowAnonymous] // Temporarily allow anonymous access for testing TODO
        public async Task<ActionResult<PaginatedResult<OrganizationListDto>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var organizations = await _mediator.Send(new GetOrganizationListRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            });
            return Ok(organizations);
        }

        // GET: api/v1/<OrganizationController>/my
        [HttpGet("my")]
        [EndpointSummary("Get my Organizations")]
        [EndpointDescription("Get a paginated list of organizations where the current user is a member. Default page size is 20, max is 100.")]
        [Authorize]
        public async Task<ActionResult<PaginatedResult<OrganizationListDto>>> GetMyOrganizations([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sid")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            var organizations = await _mediator.Send(new GetMyOrganizationsRequest
            {
                UserId = userId,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
            return Ok(organizations);
        }

        // GET api/<OrganizationController>/5
        [HttpGet("{id}")]
        [EndpointSummary("Get Organization Details")]
        [EndpointDescription("Get Details of the Organization!")]
        [AllowAnonymous]
        public async Task<ActionResult<OrganizationDto>> GetById(Guid id)
        {
            var organization = await _mediator.Send(new GetOrganizationDetailsRequest { Id = id });
            return Ok(organization);
        }

        // POST api/<OrganizationController>
        [HttpPost]
        [EndpointSummary("Create Organization")]
        [EndpointDescription("Create a new Organization")]
        [Authorize]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateOrganizationDto organization)
        {
            // Debug: Log all claims
            var claims = _httpContextAccessor.HttpContext?.User?.Claims;
            if (claims != null)
            {
                foreach (var claim in claims)
                {
                    Console.WriteLine($"Claim Type: {claim.Type}, Value: {claim.Value}");
                }
            }

            // Try multiple possible claim types for user ID
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sid")?.Value;

            Console.WriteLine($"Final UserId: {userId}");

            var command = new CreateOrganizationCommand()
            {
                OrganizationDto = organization,
                UserId = userId
            };
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        // PUT api/<OrganizationController>/5
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Put(Guid id, [FromBody] UpdateOrganizationDto updateDto)
        {
            // Get the user ID from the token
            var userId = User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            var command = new UpdateOrganizationDetailsCommand
            {
                Id = id,
                UserId = userId,
                OrganizationDto = updateDto
            };

            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        // PUT api/<OrganizationController>/updatestatustype/5
        [HttpPut("updatestatustype/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UpdateStatusType(Guid id, [FromBody] UpdateOrganizationApprovalStatusDto organizationApprovalStatus)
        {
            var command = new UpdateOrganizationCommand() { Id = id, OrganizationApprovalStatusDto = organizationApprovalStatus };
            await _mediator.Send(command);
            return NoContent();
        }

        // DELETE api/<OrganizationController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}

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
    [Route("api/[controller]")]
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
        [EndpointSummary("Get all Organizationss")]
        [EndpointDescription("Get A List of all the Organizations (pagination!)")]
        [Authorize] // TODO TEMPORARY! Needs to only allow ADMIN role but for developmenet purposes, cause currently need to manually put user as admin in Keycloak Admin console...
        public async Task<ActionResult<List<OrganizationListDto>>> GetAll()
        {
            var organizations = await _mediator.Send(new GetOrganizationListRequest());
            return Ok(organizations);
        }

        // GET api/<OrganizationController>/5
        [HttpGet("{id}")]
        [EndpointSummary("Get Organization Details")]
        [EndpointDescription("Get Details of the Organization!")]
        [AllowAnonymous]
        public async Task<ActionResult<OrganizationDto>> GetById(Guid id)
        {
            var organization = await _mediator.Send(new GetOrganizationDetailsRequest());
            return Ok(organization);
        }

        // POST api/<OrganizationController>
        [HttpPost]
        [EndpointSummary("Create Organization")]
        [EndpointDescription("Create a new Organization")]
        [AllowAnonymous]  // temporary: creation without authentification
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateOrganizationDto organization)
        {
            var command = new CreateOrganizationCommand() { OrganizationDto = organization };
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        // PUT api/<OrganizationController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // PUT api/<OrganizationController>/updatestatustype/5
        [HttpPut("updatestatustype/{id}")]
        [Authorize]
        //[Authorize(Roles = "Admin")]
        public async Task<ActionResult> UpdateStatusType(Guid id, [FromBody] UpdateOrganizationStatusTypeDto organizationStatusType)
        {
            var command = new UpdateOrganizationCommand() { Id = id, OrganizationStatusTypeDto = organizationStatusType };
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

using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Application.DTOs.OrganizationRole;
using Explore.Application.Features.OrganizationRoles.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class OrganizationRoleController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OrganizationRoleController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/v1/organizationrole
        [HttpGet]
        [EndpointSummary("Get all Organization Roles")]
        [EndpointDescription("Retrieve a list of all organization roles (Owner, Admin, Member)")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<OrganizationRoleListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<OrganizationRoleListDto>>> GetAll()
        {
            var organizationRoles = await _mediator.Send(new GetOrganizationRoleListRequest());
            return Ok(organizationRoles);
        }

        // GET: api/v1/organizationrole/{id}
        [HttpGet("{id}")]
        [EndpointSummary("Get Organization Role by ID")]
        [EndpointDescription("Retrieve details of a specific organization role")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(OrganizationRoleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrganizationRoleDto>> GetById(int id)
        {
            var organizationRole = await _mediator.Send(new GetOrganizationRoleDetailsRequest { Id = id });
            return Ok(organizationRole);
        }
    }
}

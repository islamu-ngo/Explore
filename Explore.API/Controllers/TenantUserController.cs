using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Application.DTOs.TenantUser;
using Explore.Application.Features.TenantUsers.Requests.Commands;
using Explore.Application.Features.TenantUsers.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class TenantUserController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TenantUserController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/v1/tenantuser
        [HttpGet]
        [EndpointSummary("Get all Tenant Users")]
        [EndpointDescription("Retrieve a list of all tenant users")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(List<TenantUserListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<TenantUserListDto>>> GetAll()
        {
            var tenantUsers = await _mediator.Send(new GetTenantUserListRequest());
            return Ok(tenantUsers);
        }

        // GET: api/v1/tenantuser/{id}
        [HttpGet("{id}")]
        [EndpointSummary("Get Tenant User by ID")]
        [EndpointDescription("Retrieve details of a specific tenant user")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(TenantUserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TenantUserDto>> GetById(Guid id)
        {
            var tenantUser = await _mediator.Send(new GetTenantUserDetailsRequest { Id = id });
            if (tenantUser == null)
            {
                return NotFound();
            }

            return Ok(tenantUser);
        }

        // POST: api/v1/tenantuser
        [HttpPost]
        [EndpointSummary("Create new Tenant User")]
        [EndpointDescription("Create a new tenant user association")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(BaseCommandResponse<int>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BaseCommandResponse<int>>> Create([FromBody] CreateTenantUserDto dto)
        {
            var command = new CreateTenantUserCommand { TenantUserDto = dto };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        // PUT: api/v1/tenantuser/{id}
        [HttpPut("{id}")]
        [EndpointSummary("Update Tenant User")]
        [EndpointDescription("Update an existing tenant user association")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateTenantUserDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(new { error = "Tenant User ID mismatch" });
            }

            var command = new UpdateTenantUserCommand { TenantUserDto = dto };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        // DELETE: api/v1/tenantuser/{id}
        [HttpDelete("{id}")]
        [EndpointSummary("Delete Tenant User")]
        [EndpointDescription("Delete a tenant user association")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(Guid id)
        {
            var command = new DeleteTenantUserCommand { Id = id };
            var result = await _mediator.Send(command);

            if (!result)
            {
                return NotFound(new { error = "Tenant User not found" });
            }

            return NoContent();
        }
    }
}

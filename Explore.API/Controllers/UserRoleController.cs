using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Application.DTOs.UserRole;
using Explore.Application.Features.UserRoles.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class UserRoleController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UserRoleController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/v1/userrole
        [HttpGet]
        [EndpointSummary("Get all User Roles")]
        [EndpointDescription("Retrieve a list of all user roles (Admin, Moderator, User, etc.)")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<UserRoleListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<UserRoleListDto>>> GetAll()
        {
            var userRoles = await _mediator.Send(new GetUserRoleListRequest());
            return Ok(userRoles);
        }

        // GET: api/v1/userrole/{id}
        [HttpGet("{id}")]
        [EndpointSummary("Get User Role by ID")]
        [EndpointDescription("Retrieve details of a specific user role")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(UserRoleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserRoleDto>> GetById(int id)
        {
            var userRole = await _mediator.Send(new GetUserRoleDetailsRequest { Id = id });
            if (userRole == null)
            {
                return NotFound();
            }

            return Ok(userRole);
        }
    }
}

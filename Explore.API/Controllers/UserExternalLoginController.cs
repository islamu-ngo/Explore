using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Application.DTOs.UserExternalLogin;
using Explore.Application.Features.UserExternalLogins.Requests.Commands;
using Explore.Application.Features.UserExternalLogins.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class UserExternalLoginController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UserExternalLoginController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/v1/userexternallogin
        [HttpGet]
        [EndpointSummary("Get all User External Logins")]
        [EndpointDescription("Retrieve a list of all user external logins")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(List<UserExternalLoginListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<UserExternalLoginListDto>>> GetAll()
        {
            var logins = await _mediator.Send(new GetUserExternalLoginListRequest());
            return Ok(logins);
        }

        // GET: api/v1/userexternallogin/{id}
        [HttpGet("{id}")]
        [EndpointSummary("Get User External Login by ID")]
        [EndpointDescription("Retrieve details of a specific user external login")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(UserExternalLoginDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserExternalLoginDto>> GetById(Guid id)
        {
            var login = await _mediator.Send(new GetUserExternalLoginDetailsRequest { Id = id });
            if (login == null)
            {
                return NotFound();
            }

            return Ok(login);
        }

        // POST: api/v1/userexternallogin
        [HttpPost]
        [EndpointSummary("Create new User External Login")]
        [EndpointDescription("Create a new user external login")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateUserExternalLoginDto dto)
        {
            var command = new CreateUserExternalLoginCommand { UserExternalLoginDto = dto };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        // PUT: api/v1/userexternallogin/{id}
        [HttpPut("{id}")]
        [EndpointSummary("Update User External Login")]
        [EndpointDescription("Update an existing user external login")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateUserExternalLoginDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(new { error = "User External Login ID mismatch" });
            }

            var command = new UpdateUserExternalLoginCommand { UserExternalLoginDto = dto };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        // DELETE: api/v1/userexternallogin/{id}
        [HttpDelete("{id}")]
        [EndpointSummary("Delete User External Login")]
        [EndpointDescription("Delete a user external login")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(Guid id)
        {
            var command = new DeleteUserExternalLoginCommand { Id = id };
            var result = await _mediator.Send(command);

            if (!result)
            {
                return NotFound(new { error = "User External Login not found" });
            }

            return NoContent();
        }
    }
}

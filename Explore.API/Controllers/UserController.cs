using Explore.Application.DTOs.User;
using Explore.Application.Features.Users.Requests.Commands;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Explore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UserController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("sync")]
        [Authorize]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> SyncUser()
        {
            var userId = User.FindFirst("sub")?.Value 
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var guidUserId))
            {
                return BadRequest("Invalid User ID in token");
            }

            var email = User.FindFirst("email")?.Value 
                        ?? User.FindFirst(ClaimTypes.Email)?.Value ?? "";
            
            var firstName = User.FindFirst("given_name")?.Value 
                            ?? User.FindFirst(ClaimTypes.GivenName)?.Value ?? "";
            
            var lastName = User.FindFirst("family_name")?.Value 
                           ?? User.FindFirst(ClaimTypes.Surname)?.Value ?? "";
            
            var username = User.FindFirst("preferred_username")?.Value 
                           ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "";

            var userDto = new UserDto
            {
                Id = guidUserId,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Username = username
            };

            var command = new SyncUserCommand { UserDto = userDto };
            var response = await _mediator.Send(command);

            return Ok(response);
        }
    }
}

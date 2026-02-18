using System.Security.Claims;
using Asp.Versioning;
using Explore.Application.DTOs.Organization;
using Explore.Application.DTOs.User;
using Explore.Application.Features.Users.Requests.Commands;
using Explore.Application.Features.Users.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Syncs the authenticated user from Keycloak to the local database.
    /// Creates a new User and Actor if they don't exist, otherwise updates the user's basic info.
    /// Call this endpoint after login/registration to ensure user exists in the system.
    /// </summary>
    [HttpPost("sync")]
    [Authorize]
    [EndpointSummary("Sync user from Keycloak")]
    [EndpointDescription("Creates or updates the user in the local database. Also creates the user's personal Actor if new user. Call this after login/registration.")]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> SyncUser(CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst("sub")?.Value
                     ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var guidUserId))
        {
            return BadRequest(new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = "Invalid User ID in token",
                Errors = new List<string> { "Could not parse user ID from authentication token." }
            });
        }

        var email = User.FindFirst("email")?.Value
                    ?? User.FindFirst(ClaimTypes.Email)?.Value ?? "";

        var firstName = User.FindFirst("given_name")?.Value
                        ?? User.FindFirst(ClaimTypes.GivenName)?.Value ?? "";

        var lastName = User.FindFirst("family_name")?.Value
                       ?? User.FindFirst(ClaimTypes.Surname)?.Value ?? "";

        var username = User.FindFirst("preferred_username")?.Value
                       ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "";

        // Validate required fields
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = "Email is required",
                Errors = new List<string> { "Email claim not found in token." }
            });
        }

        var userDto = new UserDto
        {
            Id = guidUserId,
            Email = email,
            FirstName = string.IsNullOrWhiteSpace(firstName) ? "User" : firstName,
            LastName = string.IsNullOrWhiteSpace(lastName) ? "" : lastName,
            Username = username
        };

        var command = new SyncUserCommand { UserDto = userDto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst("sub")?.Value
                     ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var guidUserId))
        {
            return BadRequest("Invalid User ID in token");
        }

        var query = new GetUserRequest { UserId = guidUserId };
        var user = await _mediator.Send(query, cancellationToken);

        // FIX: Return 404 if user doesn't exist
        if (user == null)
        {
            Console.WriteLine($"[USER API] User not found in database: {guidUserId}");
            return NotFound(new
            {
                message = "User not found in database. Please refresh the page to sync your profile.",
                userId = guidUserId
            });
        }

        Console.WriteLine($"[USER API] User found: {user.Email}");
        return Ok(user);
    }

    /// <summary>
    /// Gets all organizations the specified user is a member of.
    /// Returns the user's role in each organization.
    /// </summary>
    [HttpGet("{userId:guid}/organizations")]
    [Authorize]
    [EndpointSummary("Get user's organizations")]
    [EndpointDescription("Gets all organizations the user is a member of, including their role in each organization.")]
    public async Task<ActionResult<List<OrganizationListDto>>> GetUserOrganizations(Guid userId, CancellationToken cancellationToken = default)
    {
        // Verify the user is requesting their own organizations
        var currentUserId = User.FindFirst("sub")?.Value
                     ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var guidCurrentUserId))
        {
            return Unauthorized("Invalid User ID in token");
        }

        // For now, only allow users to get their own organizations
        // TODO: Add admin check for viewing other users' organizations
        if (userId != guidCurrentUserId)
        {
            return Forbid("You can only view your own organizations");
        }

        Console.WriteLine($"[USER API] Getting organizations for user: {userId}");

        var query = new GetUserOrganizationsRequest { UserId = userId };
        var organizations = await _mediator.Send(query, cancellationToken);

        Console.WriteLine($"[USER API] Found {organizations.Count} organizations for user {userId}");

        return Ok(organizations);
    }

    [HttpPut]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateUser([FromBody] UpdateUserDto userDto, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst("sub")?.Value
                     ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var guidUserId))
        {
            return BadRequest("Invalid User ID in token");
        }

        if (userDto.Id != guidUserId)
        {
            return BadRequest("User ID mismatch");
        }

        var command = new UpdateUserCommand { UpdateUserDto = userDto };
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpDelete]
    [Authorize]
    public async Task<ActionResult> DeleteUser(CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst("sub")?.Value
                     ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var guidUserId))
        {
            return BadRequest("Invalid User ID in token");
        }

        var command = new DeleteUserCommand { UserId = guidUserId };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }
}

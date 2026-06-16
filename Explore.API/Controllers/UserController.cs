// ABOUTME: REST API controller for authenticated user profile operations and preferences.
// ABOUTME: Manages user account data, profile updates, and user-specific settings.

using System.Security.Claims;
using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Hateoas;
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
[EndpointClassification(EndpointClass.Authenticated)]
public class UserController : ExploreControllerBase
{
    private readonly IMediator _mediator;

    private static readonly ApiValidationProblemDescriptor SyncValidationProblem = new(
        "user",
        "User synchronization validation failed",
        "User synchronization failed.");

    private static readonly ApiValidationProblemDescriptor UpdateValidationProblem = new(
        "user",
        "User validation failed",
        "User update failed.");

    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Syncs the authenticated user from the active identity provider to the local database.
    /// Creates a new User and Actor if they don't exist, otherwise updates the user's basic info.
    /// Call this endpoint after login/registration to ensure user exists in the system.
    /// </summary>
    [HttpPost("sync", Name = RouteNames.SyncUser)]
    [Authorize]
    [EndpointSummary("Sync user from identity provider")]
    [EndpointDescription("Creates or updates the user in the local database and ensures external provider linkage. Also creates the user's personal Actor if new user. Call this after login/registration.")]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> SyncUser(CancellationToken cancellationToken = default)
    {
        var providerSubject = User.FindFirst("sub")?.Value
                             ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst("sid")?.Value;

        if (string.IsNullOrWhiteSpace(providerSubject))
        {
            return this.ToAuthenticationRequiredProblem(
                detail: "Could not resolve provider identity from authentication token.");
        }

        var email = User.FindFirst("email")?.Value
                    ?? User.FindFirst(ClaimTypes.Email)?.Value
                    ?? string.Empty;

        var firstName = User.FindFirst("given_name")?.Value
                        ?? User.FindFirst(ClaimTypes.GivenName)?.Value
                        ?? string.Empty;

        var lastName = User.FindFirst("family_name")?.Value
                       ?? User.FindFirst(ClaimTypes.Surname)?.Value
                       ?? string.Empty;

        var username = User.FindFirst("preferred_username")?.Value
                       ?? User.FindFirst(ClaimTypes.Name)?.Value
                       ?? string.Empty;

        var provider = ResolveAuthProvider();
        var providerId = ResolveProviderId(providerSubject, provider);
        var emailVerified = ResolveEmailVerified(provider, email);

        var userIdGuid = Guid.TryParse(providerSubject, out var parsedGuid)
            ? parsedGuid
            : Guid.Empty;

        var userDto = new UserDto
        {
            Id = userIdGuid,
            Email = email,
            FirstName = string.IsNullOrWhiteSpace(firstName) ? "User" : firstName,
            LastName = string.IsNullOrWhiteSpace(lastName) ? "" : lastName,
            Username = username,
            AuthProvider = provider,
            AuthProviderId = providerId,
            EmailVerified = emailVerified
        };

        var command = new SyncUserCommand { UserDto = userDto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return this.ToCommandValidationProblem(response, SyncValidationProblem);
        }

        return Ok(response);
    }

    [HttpGet(Name = RouteNames.GetCurrentUser)]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken cancellationToken = default)
    {
        var currentUserId = await ResolveCurrentUserIdAsync(cancellationToken);
        if (!currentUserId.HasValue)
        {
            return this.ToAuthenticationRequiredProblem();
        }

        var query = new GetUserRequest { UserId = currentUserId.Value };
        var user = await _mediator.Send(query, cancellationToken);

        return Ok(user);
    }

    /// <summary>
    /// Returns the admin authority of the current authenticated user.
    /// Used by the BFF claims transformation to enrich the ClaimsPrincipal with admin claims.
    /// </summary>
    [HttpGet("admin-authority", Name = RouteNames.GetCurrentUserAdminAuthority)]
    [Authorize]
    [EndpointSummary("Get current user's admin authority")]
    [EndpointDescription("Returns instance, tenant, and organization admin status for the authenticated user. Consumed by BFF claims transformation.")]
    public async Task<ActionResult<AdminAuthorityDto>> GetAdminAuthority(CancellationToken cancellationToken = default)
    {
        var currentUserId = await ResolveCurrentUserIdAsync(cancellationToken);
        if (!currentUserId.HasValue)
        {
            return this.ToAuthenticationRequiredProblem(
                detail: "The authenticated principal could not be resolved to an application user.");
        }

        var query = new GetAdminAuthorityRequest { UserId = currentUserId.Value };
        var authority = await _mediator.Send(query, cancellationToken);

        return Ok(authority);
    }

    /// <summary>
    /// Gets all organizations the specified user is a member of.
    /// Returns the user's role in each organization.
    /// </summary>
    [HttpGet("{userId:guid}/organizations", Name = RouteNames.GetUserOrganizations)]
    [Authorize]
    [EndpointSummary("Get user's organizations")]
    [EndpointDescription("Gets all organizations the user is a member of, including their role in each organization.")]
    public async Task<ActionResult<List<OrganizationListDto>>> GetUserOrganizations(Guid userId, CancellationToken cancellationToken = default)
    {
        // Verify the user is requesting their own organizations
        var currentUserId = await ResolveCurrentUserIdAsync(cancellationToken);
        if (!currentUserId.HasValue)
        {
            return this.ToAuthenticationRequiredProblem();
        }

        // For now, only allow users to get their own organizations
        // TODO: Add admin check for viewing other users' organizations
        if (userId != currentUserId.Value)
        {
            return this.ToForbiddenProblem(detail: "You can only view your own organizations.");
        }

        var query = new GetUserOrganizationsRequest { UserId = userId };
        var organizations = await _mediator.Send(query, cancellationToken);

        return Ok(organizations);
    }

    [HttpPut(Name = RouteNames.UpdateCurrentUser)]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateUser([FromBody] UpdateUserDto userDto, CancellationToken cancellationToken = default)
    {
        var currentUserId = await ResolveCurrentUserIdAsync(cancellationToken);
        if (!currentUserId.HasValue)
        {
            return this.ToAuthenticationRequiredProblem();
        }

        if (userDto.Id != currentUserId.Value)
        {
            return this.ToValidationProblem(UpdateValidationProblem, "User ID mismatch.");
        }

        var command = new UpdateUserCommand { UpdateUserDto = userDto };
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpDelete(Name = RouteNames.DeleteCurrentUser)]
    [Authorize]
    public async Task<ActionResult> DeleteUser(CancellationToken cancellationToken = default)
    {
        var currentUserId = await ResolveCurrentUserIdAsync(cancellationToken);
        if (!currentUserId.HasValue)
        {
            return this.ToAuthenticationRequiredProblem();
        }

        var command = new DeleteUserCommand { UserId = currentUserId.Value };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    private async Task<Guid?> ResolveCurrentUserIdAsync(CancellationToken cancellationToken)
    {
        return await base.ResolveCurrentUserIdAsync(_mediator, cancellationToken);
    }

}

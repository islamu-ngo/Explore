// ABOUTME: API controller for managing user authentication tokens and session management.
// ABOUTME: Provides endpoints for token refresh, revocation, and listing active sessions.

using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Hateoas;
using Explore.Application.DTOs.UserAuthenticationToken;
using Explore.Application.Features.UserAuthenticationTokens.Requests.Commands;
using Explore.Application.Features.UserAuthenticationTokens.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[EndpointClassification(EndpointClass.Authenticated)]
public class UserAuthenticationTokenController : ControllerBase
{
    private static readonly ApiValidationProblemDescriptor CreateValidationProblem = new(
        "userAuthenticationToken",
        "User authentication token validation failed",
        "User authentication token creation failed.");

    private static readonly ApiValidationProblemDescriptor UpdateValidationProblem = new(
        "userAuthenticationToken",
        "User authentication token validation failed",
        "User authentication token update failed.");

    private readonly IMediator _mediator;

    public UserAuthenticationTokenController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/userauthenticationtoken
    [HttpGet(Name = RouteNames.GetUserAuthenticationTokens)]
    [EndpointSummary("Get all User Authentication Tokens")]
    [EndpointDescription("Retrieve a list of all user authentication tokens")]
    [Authorize]
    [ProducesResponseType(typeof(List<UserAuthenticationTokenListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<List<UserAuthenticationTokenListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var tokens = await _mediator.Send(new GetUserAuthenticationTokenListRequest(), cancellationToken);
        return Ok(tokens);
    }

    // GET: api/userauthenticationtoken/{id}
    [HttpGet("{id}", Name = RouteNames.GetUserAuthenticationTokenById)]
    [EndpointSummary("Get User Authentication Token by ID")]
    [EndpointDescription("Retrieve details of a specific user authentication token")]
    [Authorize]
    [ProducesResponseType(typeof(UserAuthenticationTokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<UserAuthenticationTokenDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var token = await _mediator.Send(new GetUserAuthenticationTokenDetailsRequest { Id = id }, cancellationToken);

        return Ok(token);
    }

    // POST: api/userauthenticationtoken
    [HttpPost(Name = RouteNames.CreateUserAuthenticationToken)]
    [EndpointSummary("Create new User Authentication Token")]
    [EndpointDescription("Create a new user authentication token")]
    [Authorize]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateUserAuthenticationTokenDto dto, CancellationToken cancellationToken = default)
    {
        var command = new CreateUserAuthenticationTokenCommand { UserAuthenticationTokenDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return this.ToCommandValidationProblem(response, CreateValidationProblem);
        }

        return Ok(response);
    }

    // PUT: api/userauthenticationtoken/{id}
    [HttpPut("{id}", Name = RouteNames.UpdateUserAuthenticationToken)]
    [EndpointSummary("Update User Authentication Token")]
    [EndpointDescription("Update an existing user authentication token")]
    [Authorize]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateUserAuthenticationTokenDto dto, CancellationToken cancellationToken = default)
    {
        if (id != dto.Id)
        {
            return this.ToValidationProblem(UpdateValidationProblem, "User authentication token ID mismatch.");
        }

        var command = new UpdateUserAuthenticationTokenCommand { UserAuthenticationTokenDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return this.ToCommandValidationProblem(response, UpdateValidationProblem);
        }

        return Ok(response);
    }

    // DELETE: api/userauthenticationtoken/{id}
    [HttpDelete("{id}", Name = RouteNames.DeleteUserAuthenticationToken)]
    [EndpointSummary("Delete User Authentication Token")]
    [EndpointDescription("Delete a user authentication token")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteUserAuthenticationTokenCommand { Id = id };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }
}

// ABOUTME: API controller for managing external login provider configurations and user external identities.
// ABOUTME: Handles OAuth/OIDC provider linking, unlinking, and identity verification flows.

using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Hateoas;
using Explore.Application.DTOs.UserExternalLogin;
using Explore.Application.Features.UserExternalLogins.Requests.Commands;
using Explore.Application.Features.UserExternalLogins.Requests.Queries;
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
public class UserExternalLoginController : ControllerBase
{
    private static readonly ApiValidationProblemDescriptor CreateValidationProblem = new(
        "userExternalLogin",
        "User external login validation failed",
        "User external login creation failed.");

    private static readonly ApiValidationProblemDescriptor UpdateValidationProblem = new(
        "userExternalLogin",
        "User external login validation failed",
        "User external login update failed.");

    private static readonly ApiNotFoundProblemDescriptor UserExternalLoginNotFoundProblem = new(
        "User external login not found",
        "User external login not found.");

    private readonly IMediator _mediator;

    public UserExternalLoginController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/userexternallogin
    [HttpGet(Name = RouteNames.GetUserExternalLogins)]
    [EndpointSummary("Get all User External Logins")]
    [EndpointDescription("Retrieve a list of all user external logins")]
    [Authorize]
    [ProducesResponseType(typeof(List<UserExternalLoginListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<List<UserExternalLoginListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var logins = await _mediator.Send(new GetUserExternalLoginListRequest(), cancellationToken);
        return Ok(logins);
    }

    // GET: api/userexternallogin/{id}
    [HttpGet("{id}", Name = RouteNames.GetUserExternalLoginById)]
    [EndpointSummary("Get User External Login by ID")]
    [EndpointDescription("Retrieve details of a specific user external login")]
    [Authorize]
    [ProducesResponseType(typeof(UserExternalLoginDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<UserExternalLoginDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var login = await _mediator.Send(new GetUserExternalLoginDetailsRequest { Id = id }, cancellationToken);

        return Ok(login);
    }

    // POST: api/userexternallogin
    [HttpPost(Name = RouteNames.CreateUserExternalLogin)]
    [EndpointSummary("Create new User External Login")]
    [EndpointDescription("Create a new user external login")]
    [Authorize]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateUserExternalLoginDto dto, CancellationToken cancellationToken = default)
    {
        var command = new CreateUserExternalLoginCommand { UserExternalLoginDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return this.ToCommandValidationProblem(response, CreateValidationProblem);
        }

        return Ok(response);
    }

    // PUT: api/userexternallogin/{id}
    [HttpPut("{id}", Name = RouteNames.UpdateUserExternalLogin)]
    [EndpointSummary("Update User External Login")]
    [EndpointDescription("Update an existing user external login")]
    [Authorize]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateUserExternalLoginDto dto, CancellationToken cancellationToken = default)
    {
        if (id != dto.Id)
        {
            return this.ToValidationProblem(UpdateValidationProblem, "User external login ID mismatch.");
        }

        var command = new UpdateUserExternalLoginCommand { UserExternalLoginDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return string.Equals(response.Message, "User External Login not found.", StringComparison.Ordinal)
                ? this.ToNotFoundProblem(UserExternalLoginNotFoundProblem)
                : this.ToCommandValidationProblem(response, UpdateValidationProblem);
        }

        return Ok(response);
    }

    // DELETE: api/userexternallogin/{id}
    [HttpDelete("{id}", Name = RouteNames.DeleteUserExternalLogin)]
    [EndpointSummary("Delete User External Login")]
    [EndpointDescription("Delete a user external login")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteUserExternalLoginCommand { Id = id };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }
}

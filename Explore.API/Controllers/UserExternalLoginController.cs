// ABOUTME: API controller for managing external login provider configurations and user external identities.
// ABOUTME: Handles OAuth/OIDC provider linking, unlinking, and identity verification flows.

using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using Explore.API.Attributes;
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
    private const string ValidationFailedCode = "validation_failed";
    private const string ResourceNotFoundCode = "resource_not_found";

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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateUserExternalLoginDto dto, CancellationToken cancellationToken = default)
    {
        var command = new CreateUserExternalLoginCommand { UserExternalLoginDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
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
            return ToValidationProblem(
                new BaseCommandResponse<Guid>
                {
                    Success = false,
                    Message = "User external login ID mismatch.",
                    FailureCode = ValidationFailedCode,
                    Errors = ["User external login ID mismatch."]
                },
                "User external login ID mismatch.");
        }

        var command = new UpdateUserExternalLoginCommand { UserExternalLoginDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return string.Equals(response.Message, "User External Login not found.", StringComparison.Ordinal)
                ? ToNotFoundProblem(response, "User external login not found.")
                : ToValidationProblem(response, "User external login update failed.");
        }

        return Ok(response);
    }

    // DELETE: api/userexternallogin/{id}
    [HttpDelete("{id}", Name = RouteNames.DeleteUserExternalLogin)]
    [EndpointSummary("Delete User External Login")]
    [EndpointDescription("Delete a user external login")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteUserExternalLoginCommand { Id = id };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    private ActionResult ToValidationProblem<TKey>(BaseCommandResponse<TKey> response, string fallbackDetail)
    {
        var errors = response.Errors is { Count: > 0 }
            ? response.Errors.ToArray()
            : [response.Message ?? fallbackDetail];

        var problemDetails = new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["userExternalLogin"] = errors
        })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "User external login validation failed",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Detail = response.Message ?? fallbackDetail,
            Instance = HttpContext.Request.Path
        };

        problemDetails.Extensions["code"] = string.IsNullOrWhiteSpace(response.FailureCode)
            ? ValidationFailedCode
            : response.FailureCode;
        AddProblemDetailsExtensions(problemDetails);

        return new ObjectResult(problemDetails)
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentTypes = { "application/problem+json" }
        };
    }

    private ActionResult ToNotFoundProblem<TKey>(BaseCommandResponse<TKey> response, string fallbackDetail)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "User external login not found",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
            Detail = response.Message ?? fallbackDetail,
            Instance = HttpContext.Request.Path
        };

        problemDetails.Extensions["code"] = string.IsNullOrWhiteSpace(response.FailureCode)
            ? ResourceNotFoundCode
            : response.FailureCode;
        AddProblemDetailsExtensions(problemDetails);

        return new ObjectResult(problemDetails)
        {
            StatusCode = StatusCodes.Status404NotFound,
            ContentTypes = { "application/problem+json" }
        };
    }

    private void AddProblemDetailsExtensions(ProblemDetails problemDetails)
    {
        problemDetails.Extensions["traceId"] = HttpContext.TraceIdentifier;
        problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;

        if (HttpContext.Items["CorrelationId"] is string correlationId)
        {
            problemDetails.Extensions["correlationId"] = correlationId;
        }
    }
}

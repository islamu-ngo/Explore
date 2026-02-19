using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
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
public class UserAuthenticationTokenController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserAuthenticationTokenController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/userauthenticationtoken
    [HttpGet]
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
    [HttpGet("{id}")]
    [EndpointSummary("Get User Authentication Token by ID")]
    [EndpointDescription("Retrieve details of a specific user authentication token")]
    [Authorize]
    [ProducesResponseType(typeof(UserAuthenticationTokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<UserAuthenticationTokenDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var token = await _mediator.Send(new GetUserAuthenticationTokenDetailsRequest { Id = id }, cancellationToken);
        if (token == null)
        {
            return NotFound();
        }

        return Ok(token);
    }

    // POST: api/userauthenticationtoken
    [HttpPost]
    [EndpointSummary("Create new User Authentication Token")]
    [EndpointDescription("Create a new user authentication token")]
    [Authorize]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateUserAuthenticationTokenDto dto, CancellationToken cancellationToken = default)
    {
        var command = new CreateUserAuthenticationTokenCommand { UserAuthenticationTokenDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    // PUT: api/userauthenticationtoken/{id}
    [HttpPut("{id}")]
    [EndpointSummary("Update User Authentication Token")]
    [EndpointDescription("Update an existing user authentication token")]
    [Authorize]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateUserAuthenticationTokenDto dto, CancellationToken cancellationToken = default)
    {
        if (id != dto.Id)
        {
            return BadRequest(new { error = "User Authentication Token ID mismatch" });
        }

        var command = new UpdateUserAuthenticationTokenCommand { UserAuthenticationTokenDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    // DELETE: api/userauthenticationtoken/{id}
    [HttpDelete("{id}")]
    [EndpointSummary("Delete User Authentication Token")]
    [EndpointDescription("Delete a user authentication token")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteUserAuthenticationTokenCommand { Id = id };
        var result = await _mediator.Send(command, cancellationToken);

        if (!result)
        {
            return NotFound(new { error = "User Authentication Token not found" });
        }

        return NoContent();
    }
}

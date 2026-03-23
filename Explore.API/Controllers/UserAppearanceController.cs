// ABOUTME: API controller for authenticated user appearance preferences.
// ABOUTME: Exposes the effective appearance settings and server-authoritative preference updates for the current user.

using Asp.Versioning;
using Explore.Application.DTOs.Appearance;
using Explore.Application.Features.Appearance.Requests.Commands;
using Explore.Application.Features.Appearance.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/user/appearance")]
[ApiController]
[Authorize]
public class UserAppearanceController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserAppearanceController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet(Name = Hateoas.RouteNames.GetCurrentUserAppearancePreferences)]
    [EndpointSummary("Get Current User Appearance Preferences")]
    [EndpointDescription("Returns the effective appearance preferences for the authenticated user after hierarchical resolution.")]
    [ProducesResponseType(typeof(UserAppearancePreferencesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserAppearancePreferencesDto>> Get(CancellationToken cancellationToken = default)
    {
        var preferences = await _mediator.Send(new GetCurrentUserAppearancePreferencesQuery(), cancellationToken);
        return Ok(preferences);
    }

    [HttpPut(Name = Hateoas.RouteNames.UpdateCurrentUserAppearancePreferences)]
    [EndpointSummary("Update Current User Appearance Preferences")]
    [EndpointDescription("Updates the authenticated user's appearance preferences and persists sparse user overrides.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(
        [FromBody] UpdateUserAppearancePreferencesDto preferences,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new UpdateCurrentUserAppearancePreferencesCommand
        {
            Preferences = preferences
        }, cancellationToken);

        if (response.Success)
        {
            return Ok(response);
        }

        if (response.Errors?.Count > 0)
        {
            return BadRequest(response);
        }

        return Unauthorized(response);
    }
}

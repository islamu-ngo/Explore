// ABOUTME: Admin API controller for managing the UI theme catalog (platform and tenant-owned themes).
// ABOUTME: Authorization is enforced per-theme inside handlers using IAdminContext to gate platform vs. tenant scope.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Hateoas;
using Explore.Application.DTOs.Appearance;
using Explore.Application.Features.Appearance.Requests.Commands;
using Explore.Application.Features.Appearance.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/admin/ui-themes")]
[ApiController]
[Authorize]
[EndpointClassification(EndpointClass.Authenticated)]
public class UiThemeAdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public UiThemeAdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet(Name = RouteNames.GetUiThemeCatalog)]
    [EndpointSummary("Get UI Theme Catalog")]
    [EndpointDescription("Returns the platform or tenant-owned UI theme catalog. Scope is controlled by the isPlatformCatalog query parameter.")]
    [ProducesResponseType(typeof(IReadOnlyList<UiThemeListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<UiThemeListItemDto>>> GetCatalog(
        [FromQuery] bool isPlatformCatalog = false,
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        var themes = await _mediator.Send(new GetUiThemeCatalogQuery
        {
            IsPlatformCatalog = isPlatformCatalog,
            ActiveOnly = activeOnly,
        }, cancellationToken);

        return Ok(themes);
    }

    [HttpGet("{id:guid}", Name = RouteNames.GetUiThemeDetails)]
    [EndpointSummary("Get UI Theme Details")]
    [EndpointDescription("Returns full UI theme details including light/dark palettes for administrative editing.")]
    [ProducesResponseType(typeof(UiThemeDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UiThemeDetailsDto>> GetDetails(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var theme = await _mediator.Send(new GetUiThemeDetailsQuery { Id = id }, cancellationToken);
        if (theme is null)
        {
            return NotFound();
        }

        return Ok(theme);
    }

    [HttpPost(Name = RouteNames.CreateUiTheme)]
    [EndpointSummary("Create UI Theme")]
    [EndpointDescription("Creates a platform or tenant-owned UI theme. Platform themes require instance admin privileges.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create(
        [FromBody] CreateUiThemeDto dto,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new CreateUiThemeCommand { UiThemeDto = dto }, cancellationToken);

        if (response.Success)
        {
            return CreatedAtRoute(RouteNames.GetUiThemeDetails, new { id = response.Id }, response);
        }

        if (response.Errors?.Count > 0)
        {
            return BadRequest(response);
        }

        return Unauthorized(response);
    }

    [HttpPut("{id:guid}", Name = RouteNames.UpdateUiTheme)]
    [EndpointSummary("Update UI Theme")]
    [EndpointDescription("Updates an existing UI theme. The caller must match the theme's scope (platform or tenant) authorization.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateUiThemeDto dto,
        CancellationToken cancellationToken = default)
    {
        if (id != dto.Id)
        {
            return BadRequest(new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = "Route id does not match payload id.",
                Errors = ["The theme id in the URL must match the id in the request body."]
            });
        }

        var response = await _mediator.Send(new UpdateUiThemeCommand { UiThemeDto = dto }, cancellationToken);

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

    [HttpDelete("{id:guid}", Name = RouteNames.DeleteUiTheme)]
    [EndpointSummary("Delete UI Theme")]
    [EndpointDescription("Deletes a UI theme. The theme must not be marked as default for its scope; promote another theme first.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var deleted = await _mediator.Send(new DeleteUiThemeCommand { Id = id }, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}

// ABOUTME: Focused API controller for current-tenant storage administration settings.
// ABOUTME: Exposes effective storage policy and tenant override updates through CQRS.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Hateoas;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Tenant;
using Explore.Application.Features.TenantStorageSettings.Requests.Commands;
using Explore.Application.Features.TenantStorageSettings.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/tenant/settings/storage")]
[ApiController]
[Authorize]
[EndpointClassification(EndpointClass.Authenticated)]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public sealed class TenantStorageSettingsController(
    IMediator mediator,
    IResourceAssembler<TenantStorageSettingsDto, TenantStorageSettingsDto> storageSettingsAssembler)
    : ExploreControllerBase
{
    [HttpGet("", Name = RouteNames.GetTenantStorageSettings)]
    [EndpointSummary("Get Tenant Storage Settings")]
    [EndpointDescription("Returns effective tenant storage policy, usage, lock state, and redacted optional S3 override settings.")]
    [ProducesResponseType(typeof(HalResource<TenantStorageSettingsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<HalResource<TenantStorageSettingsDto>>> GetStorageSettings(
        CancellationToken cancellationToken = default)
    {
        var settings = await mediator.Send(new GetTenantStorageSettingsQuery(), cancellationToken);
        var halResource = await storageSettingsAssembler.ToResource(settings, HttpContext);
        return Ok(halResource);
    }

    [HttpPut("", Name = RouteNames.UpdateTenantStorageSettings)]
    [EndpointSummary("Update Tenant Storage Settings")]
    [EndpointDescription("Updates current-tenant storage overrides when instance storage delegation allows tenant edits.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateStorageSettings(
        [FromBody] TenantStorageSettingsDto settings,
        CancellationToken cancellationToken = default)
    {
        var userId = await ResolveCurrentUserIdAsync(mediator, cancellationToken);
        if (!userId.HasValue)
        {
            return BadRequest(new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = "Invalid user identity."
            });
        }

        var response = await mediator.Send(
            new UpdateTenantStorageSettingsCommand
            {
                UserId = userId.Value,
                Settings = settings
            },
            cancellationToken);

        if (!response.Success)
        {
            if (response.Message?.Contains("administrators", StringComparison.OrdinalIgnoreCase) == true)
            {
                return Forbid();
            }

            return BadRequest(response);
        }

        return Ok(response);
    }
}

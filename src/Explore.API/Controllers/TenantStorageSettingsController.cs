// ABOUTME: Focused API controller for current-tenant storage administration settings.
// ABOUTME: Exposes effective storage policy and presence-aware tenant override patches through CQRS.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
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
    private static readonly ApiValidationProblemDescriptor PatchValidationProblem = new(
        "tenantStorageSettings",
        "Tenant storage settings validation failed",
        "Tenant storage settings patch failed.");

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

    [HttpPatch("", Name = RouteNames.PatchTenantStorageSettings)]
    [EndpointSummary("Patch Tenant Storage Settings")]
    [EndpointDescription("Patches supplied current-tenant storage override leaves when instance storage delegation allows tenant edits.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> PatchStorageSettings(
        [FromBody] PatchTenantStorageSettingsDto settings,
        CancellationToken cancellationToken = default)
    {
        var userId = await ResolveCurrentUserIdAsync(mediator, cancellationToken);
        if (!userId.HasValue)
        {
            return this.ToAuthenticationRequiredProblem(
                detail: "The authenticated principal could not be resolved to an application user.");
        }

        var response = await mediator.Send(
            new PatchTenantStorageSettingsCommand
            {
                UserId = userId.Value,
                Settings = settings
            },
            cancellationToken);

        if (!response.Success)
        {
            if (response.Message?.Contains("administrators", StringComparison.OrdinalIgnoreCase) == true)
            {
                return this.ToForbiddenProblem(
                    detail: response.Message ?? "Tenant storage settings can only be patched by authorized administrators.");
            }

            return this.ToCommandValidationProblem(response, PatchValidationProblem);
        }

        return Ok(response);
    }
}

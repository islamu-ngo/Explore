// ABOUTME: Unified REST controller for hierarchical settings management at user and tenant scopes.
// ABOUTME: Exposes generic CRUD + lock/unlock endpoints consumed by any settings UI (EventList is first consumer).

using Asp.Versioning;
using Explore.API.Hateoas;
using Explore.Application.DTOs.Settings;
using Explore.Application.Features.Settings.Requests.Commands;
using Explore.Application.Features.Settings.Requests.Queries;
using Explore.Application.Responses;
using Explore.Domain.Settings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/settings")]
[ApiController]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SettingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ── User Scope Endpoints ────────────────────────────────────────────

    [HttpGet("user/{category}", Name = RouteNames.GetUserSettings)]
    [EndpointSummary("Get User Settings")]
    [EndpointDescription("Returns effective settings for the given category, resolved through the full hierarchy for the authenticated user.")]
    [ProducesResponseType(typeof(SettingGroupResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SettingGroupResponseDto>> GetUserSettings(
        string category, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ResolveSettingGroupQuery
        {
            Category = category,
            Scope = SettingScope.User
        }, cancellationToken);

        return Ok(result);
    }

    [HttpPut("user/{category}", Name = RouteNames.UpdateUserSettingsBatch)]
    [EndpointSummary("Batch Update User Settings")]
    [EndpointDescription("Applies multiple user preference updates for a category. Defaults to best-effort mode (skips locked settings, applies rest).")]
    [ProducesResponseType(typeof(BatchUpdateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BatchUpdateResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BatchUpdateResponseDto>> UpdateUserSettingsBatch(
        string category,
        [FromBody] UpdateSettingBatchDto body,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new UpdateSettingBatchCommand
        {
            Category = category,
            Values = body.Values,
            Scope = SettingScope.User,
            Mode = body.Mode ?? BatchUpdateMode.BestEffort
        }, cancellationToken);

        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("user/keys/{key}", Name = RouteNames.UpdateUserSetting)]
    [EndpointSummary("Update Single User Setting")]
    [EndpointDescription("Updates a single user preference by key. Key must be a registered setting key.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateUserSetting(
        string key,
        [FromBody] UpdateSettingValueDto body,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new UpdateSettingCommand
        {
            Key = key,
            Value = body.Value,
            Scope = SettingScope.User
        }, cancellationToken);

        return HandleCommandResponse(response);
    }

    [HttpDelete("user/keys/{key}", Name = RouteNames.ResetUserSetting)]
    [EndpointSummary("Reset User Setting")]
    [EndpointDescription("Removes the user's override for a setting, restoring it to the next higher scope's value.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> ResetUserSetting(
        string key, CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new ResetSettingCommand
        {
            Key = key,
            Scope = SettingScope.User
        }, cancellationToken);

        return HandleCommandResponse(response);
    }

    // ── Tenant Scope Endpoints ──────────────────────────────────────────

    [HttpGet("tenant/{category}", Name = RouteNames.GetTenantScopedSettings)]
    [EndpointSummary("Get Tenant Settings")]
    [EndpointDescription("Returns effective settings for the given category at tenant scope. Requires tenant administrator.")]
    [ProducesResponseType(typeof(SettingGroupResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SettingGroupResponseDto>> GetTenantSettings(
        string category, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ResolveSettingGroupQuery
        {
            Category = category,
            Scope = SettingScope.Tenant
        }, cancellationToken);

        return Ok(result);
    }

    [HttpPut("tenant/{category}", Name = RouteNames.UpdateTenantSettingsBatch)]
    [EndpointSummary("Batch Update Tenant Settings")]
    [EndpointDescription("Applies multiple tenant setting updates for a category. Defaults to strict mode (rejects entire batch if any setting is locked). Requires tenant administrator.")]
    [ProducesResponseType(typeof(BatchUpdateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BatchUpdateResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BatchUpdateResponseDto>> UpdateTenantSettingsBatch(
        string category,
        [FromBody] UpdateSettingBatchDto body,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new UpdateSettingBatchCommand
        {
            Category = category,
            Values = body.Values,
            Scope = SettingScope.Tenant,
            Mode = body.Mode ?? BatchUpdateMode.Strict
        }, cancellationToken);

        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("tenant/keys/{key}", Name = RouteNames.UpdateTenantSetting)]
    [EndpointSummary("Update Single Tenant Setting")]
    [EndpointDescription("Updates a single tenant setting by key. Requires tenant administrator.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateTenantSetting(
        string key,
        [FromBody] UpdateSettingValueDto body,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new UpdateSettingCommand
        {
            Key = key,
            Value = body.Value,
            Scope = SettingScope.Tenant
        }, cancellationToken);

        return HandleCommandResponse(response);
    }

    [HttpPost("tenant/keys/{key}/lock", Name = RouteNames.LockTenantSetting)]
    [EndpointSummary("Lock Tenant Setting")]
    [EndpointDescription("Locks a setting at tenant scope, preventing lower-scope overrides from taking effect. Requires tenant administrator.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> LockTenantSetting(
        string key, CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new LockSettingCommand
        {
            Key = key,
            Scope = SettingScope.Tenant
        }, cancellationToken);

        return HandleCommandResponse(response);
    }

    [HttpDelete("tenant/keys/{key}/lock", Name = RouteNames.UnlockTenantSetting)]
    [EndpointSummary("Unlock Tenant Setting")]
    [EndpointDescription("Unlocks a setting at tenant scope, restoring the normal hierarchical cascade. Requires tenant administrator.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UnlockTenantSetting(
        string key, CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new UnlockSettingCommand
        {
            Key = key,
            Scope = SettingScope.Tenant
        }, cancellationToken);

        return HandleCommandResponse(response);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private ActionResult<BaseCommandResponse<Guid>> HandleCommandResponse(BaseCommandResponse<Guid> response)
    {
        if (response.Success) return Ok(response);

        if (response.Message?.Contains("administrators", StringComparison.OrdinalIgnoreCase) == true)
            return Forbid();

        return BadRequest(response);
    }
}

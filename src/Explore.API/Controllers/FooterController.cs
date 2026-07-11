// ABOUTME: API controller for tenant footer configuration — link groups, links, and scalar settings.
// ABOUTME: GET endpoints are public; write endpoints require tenant-admin authorization.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Hateoas;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.Footer;
using Explore.Application.Features.Footer.Requests.Commands;
using Explore.Application.Features.Footer.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public class FooterController : ExploreControllerBase
{
    private static readonly ApiValidationProblemDescriptor LinkGroupValidationProblem = new(
        "footerLinkGroup",
        "Footer link group validation failed",
        "Footer link group operation failed.");

    private static readonly ApiValidationProblemDescriptor LinkGroupReorderValidationProblem = new(
        "footerLinkGroup",
        "Footer link group validation failed",
        "Footer link group reorder failed.");

    private static readonly ApiValidationProblemDescriptor LinkValidationProblem = new(
        "footerLink",
        "Footer link validation failed",
        "Footer link operation failed.");

    private static readonly ApiValidationProblemDescriptor SettingsValidationProblem = new(
        "tenantFooterSettings",
        "Tenant footer settings validation failed",
        "Tenant footer settings update failed.");

    private readonly IMediator _mediator;
    private readonly ITenantContext _tenantContext;

    public FooterController(IMediator mediator, ITenantContext tenantContext)
    {
        _mediator = mediator;
        _tenantContext = tenantContext;
    }

    // ── Public / tenant-read endpoints ──────────────────────────────────────

    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("config", Name = RouteNames.GetFooterConfig)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<FooterConfigDto>> GetConfig(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFooterConfigQuery(), cancellationToken);
        return Ok(result);
    }

    // ── Link group admin endpoints ───────────────────────────────────────────

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpGet("link-groups", Name = RouteNames.GetFooterLinkGroups)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FooterLinkGroupListDto>>> GetLinkGroups(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFooterLinkGroupListQuery(), cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpGet("link-groups/{id:guid}", Name = RouteNames.GetFooterLinkGroupById)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FooterLinkGroupDetailsDto>> GetLinkGroupById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFooterLinkGroupDetailsQuery(id), cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost("link-groups", Name = RouteNames.CreateFooterLinkGroup)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> CreateLinkGroup(
        [FromBody] CreateFooterLinkGroupRequest request, CancellationToken cancellationToken)
    {
        if (TryGetCurrentUserId(out var userId) is { } unauthorized)
        {
            return unauthorized;
        }
        var result = await _mediator.Send(new CreateFooterLinkGroupCommand
        {
            UserId = userId,
            TenantId = _tenantContext.TenantId,
            Title = request.Title,
        }, cancellationToken);

        if (!result.Success)
            return this.ToCommandValidationProblem(result, LinkGroupValidationProblem);

        return CreatedAtAction(nameof(GetLinkGroupById), new { id = result.Id }, result);
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPut("link-groups/{id:guid}", Name = RouteNames.UpdateFooterLinkGroup)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateLinkGroup(
        Guid id, [FromBody] UpdateFooterLinkGroupRequest request, CancellationToken cancellationToken)
    {
        if (TryGetCurrentUserId(out var userId) is { } unauthorized)
        {
            return unauthorized;
        }
        var result = await _mediator.Send(new UpdateFooterLinkGroupCommand
        {
            UserId = userId,
            TenantId = _tenantContext.TenantId,
            GroupId = id,
            Title = request.Title,
            IsActive = request.IsActive,
        }, cancellationToken);

        if (!result.Success)
            return this.ToCommandValidationProblem(result, LinkGroupValidationProblem);

        return Ok(result);
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpDelete("link-groups/{id:guid}", Name = RouteNames.DeleteFooterLinkGroup)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> DeleteLinkGroup(Guid id, CancellationToken cancellationToken)
    {
        if (TryGetCurrentUserId(out var userId) is { } unauthorized)
        {
            return unauthorized;
        }
        var result = await _mediator.Send(new DeleteFooterLinkGroupCommand
        {
            UserId = userId,
            TenantId = _tenantContext.TenantId,
            GroupId = id
        }, cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost("link-groups/reorder", Name = RouteNames.ReorderFooterLinkGroups)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> ReorderLinkGroups(
        [FromBody] List<Guid> orderedGroupIds, CancellationToken cancellationToken)
    {
        if (TryGetCurrentUserId(out var userId) is { } unauthorized)
        {
            return unauthorized;
        }
        var result = await _mediator.Send(new ReorderFooterLinkGroupsCommand
        {
            UserId = userId,
            TenantId = _tenantContext.TenantId,
            OrderedGroupIds = orderedGroupIds,
        }, cancellationToken);

        if (!result.Success)
            return this.ToCommandValidationProblem(result, LinkGroupReorderValidationProblem);

        return Ok(result);
    }

    // ── Link admin endpoints ─────────────────────────────────────────────────

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost("link-groups/{groupId:guid}/links", Name = RouteNames.CreateFooterLink)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> CreateLink(
        Guid groupId, [FromBody] CreateFooterLinkRequest request, CancellationToken cancellationToken)
    {
        if (TryGetCurrentUserId(out var userId) is { } unauthorized)
        {
            return unauthorized;
        }
        var result = await _mediator.Send(new CreateFooterLinkCommand
        {
            UserId = userId,
            TenantId = _tenantContext.TenantId,
            GroupId = groupId,
            Label = request.Label,
            Url = request.Url,
            OpenInNewTab = request.OpenInNewTab,
        }, cancellationToken);

        if (!result.Success)
            return this.ToCommandValidationProblem(result, LinkValidationProblem);

        return Created(string.Empty, result);
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPut("links/{id:guid}", Name = RouteNames.UpdateFooterLink)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateLink(
        Guid id, [FromBody] UpdateFooterLinkRequest request, CancellationToken cancellationToken)
    {
        if (TryGetCurrentUserId(out var userId) is { } unauthorized)
        {
            return unauthorized;
        }
        var result = await _mediator.Send(new UpdateFooterLinkCommand
        {
            UserId = userId,
            TenantId = _tenantContext.TenantId,
            LinkId = id,
            Label = request.Label,
            Url = request.Url,
            OpenInNewTab = request.OpenInNewTab,
            IsActive = request.IsActive,
        }, cancellationToken);

        if (!result.Success)
            return this.ToCommandValidationProblem(result, LinkValidationProblem);

        return Ok(result);
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpDelete("links/{id:guid}", Name = RouteNames.DeleteFooterLink)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> DeleteLink(Guid id, CancellationToken cancellationToken)
    {
        if (TryGetCurrentUserId(out var userId) is { } unauthorized)
        {
            return unauthorized;
        }
        var result = await _mediator.Send(new DeleteFooterLinkCommand
        {
            UserId = userId,
            TenantId = _tenantContext.TenantId,
            LinkId = id
        }, cancellationToken);
        return Ok(result);
    }

    // ── Tenant settings endpoint ─────────────────────────────────────────────

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPut("settings", Name = RouteNames.UpdateTenantFooterSettings)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateSettings(
        [FromBody] UpdateTenantFooterSettingsRequest request, CancellationToken cancellationToken)
    {
        if (TryGetCurrentUserId(out var userId) is { } unauthorized)
        {
            return unauthorized;
        }
        var result = await _mediator.Send(new UpdateTenantFooterSettingsCommand
        {
            UserId = userId,
            TenantId = _tenantContext.TenantId,
            Enabled = request.Enabled,
            Template = request.Template,
            ShowDescription = request.ShowDescription,
            DescriptionText = request.DescriptionText,
            ShowSocialLinks = request.ShowSocialLinks,
            SocialLinksJson = request.SocialLinksJson,
            CopyrightText = request.CopyrightText,
            ShowCookieSettingsLink = request.ShowCookieSettingsLink,
        }, cancellationToken);

        if (!result.Success)
            return this.ToCommandValidationProblem(result, SettingsValidationProblem);

        return Ok(result);
    }


    private ActionResult? TryGetCurrentUserId(out Guid userId)
    {
        if (CurrentUserId is { } currentUserId)
        {
            userId = currentUserId;
            return null;
        }

        userId = Guid.Empty;
        return this.ToAuthenticationRequiredProblem();
    }

    // ── Request body types ───────────────────────────────────────────────────

    public sealed record CreateFooterLinkGroupRequest(string Title);
    public sealed record UpdateFooterLinkGroupRequest(string Title, bool IsActive);
    public sealed record CreateFooterLinkRequest(string Label, string Url, bool OpenInNewTab);
    public sealed record UpdateFooterLinkRequest(string Label, string Url, bool OpenInNewTab, bool IsActive);
    public sealed record UpdateTenantFooterSettingsRequest(
        bool? Enabled,
        string? Template,
        bool? ShowDescription,
        string? DescriptionText,
        bool? ShowSocialLinks,
        string? SocialLinksJson,
        string? CopyrightText,
        bool? ShowCookieSettingsLink);
}

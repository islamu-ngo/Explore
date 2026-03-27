// ABOUTME: API controller for tenant footer configuration — link groups, links, and scalar settings.
// ABOUTME: GET endpoints are public; write endpoints require tenant-admin authorization.

using Asp.Versioning;
using Explore.API.Hateoas;
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
public class FooterController : ControllerBase
{
    private readonly IMediator _mediator;

    public FooterController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ── Public / tenant-read endpoints ──────────────────────────────────────

    [HttpGet("config", Name = RouteNames.GetFooterConfig)]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<FooterConfigDto>> GetConfig(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFooterConfigQuery(), cancellationToken);
        return Ok(result);
    }

    // ── Link group admin endpoints ───────────────────────────────────────────

    [HttpGet("link-groups", Name = RouteNames.GetFooterLinkGroups)]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FooterLinkGroupListDto>>> GetLinkGroups(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFooterLinkGroupListQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("link-groups/{id:guid}", Name = RouteNames.GetFooterLinkGroupById)]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FooterLinkGroupDetailsDto>> GetLinkGroupById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFooterLinkGroupDetailsQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost("link-groups", Name = RouteNames.CreateFooterLinkGroup)]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> CreateLinkGroup(
        [FromBody] CreateFooterLinkGroupRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(new CreateFooterLinkGroupCommand
        {
            UserId = userId,
            Title = request.Title,
        }, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetLinkGroupById), new { id = result.Id }, result);
    }

    [HttpPut("link-groups/{id:guid}", Name = RouteNames.UpdateFooterLinkGroup)]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateLinkGroup(
        Guid id, [FromBody] UpdateFooterLinkGroupRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(new UpdateFooterLinkGroupCommand
        {
            UserId = userId,
            GroupId = id,
            Title = request.Title,
            IsActive = request.IsActive,
        }, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpDelete("link-groups/{id:guid}", Name = RouteNames.DeleteFooterLinkGroup)]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> DeleteLinkGroup(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(new DeleteFooterLinkGroupCommand { UserId = userId, GroupId = id }, cancellationToken);
        return Ok(result);
    }

    [HttpPost("link-groups/reorder", Name = RouteNames.ReorderFooterLinkGroups)]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> ReorderLinkGroups(
        [FromBody] List<Guid> orderedGroupIds, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(new ReorderFooterLinkGroupsCommand
        {
            UserId = userId,
            OrderedGroupIds = orderedGroupIds,
        }, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    // ── Link admin endpoints ─────────────────────────────────────────────────

    [HttpPost("link-groups/{groupId:guid}/links", Name = RouteNames.CreateFooterLink)]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> CreateLink(
        Guid groupId, [FromBody] CreateFooterLinkRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(new CreateFooterLinkCommand
        {
            UserId = userId,
            GroupId = groupId,
            Label = request.Label,
            Url = request.Url,
            OpenInNewTab = request.OpenInNewTab,
        }, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Created(string.Empty, result);
    }

    [HttpPut("links/{id:guid}", Name = RouteNames.UpdateFooterLink)]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateLink(
        Guid id, [FromBody] UpdateFooterLinkRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(new UpdateFooterLinkCommand
        {
            UserId = userId,
            LinkId = id,
            Label = request.Label,
            Url = request.Url,
            OpenInNewTab = request.OpenInNewTab,
            IsActive = request.IsActive,
        }, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpDelete("links/{id:guid}", Name = RouteNames.DeleteFooterLink)]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> DeleteLink(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(new DeleteFooterLinkCommand { UserId = userId, LinkId = id }, cancellationToken);
        return Ok(result);
    }

    // ── Tenant settings endpoint ─────────────────────────────────────────────

    [HttpPut("settings", Name = RouteNames.UpdateTenantFooterSettings)]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateSettings(
        [FromBody] UpdateTenantFooterSettingsRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(new UpdateTenantFooterSettingsCommand
        {
            UserId = userId,
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
            return BadRequest(result);

        return Ok(result);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var raw = HttpContext.User?.FindFirst("sub")?.Value
            ?? HttpContext.User?.FindFirst(
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
            ?? HttpContext.User?.FindFirst("sid")?.Value;

        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
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

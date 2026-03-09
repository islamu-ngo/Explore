// ABOUTME: Anonymous-safe API controller for first-party browser analytics relay transport.
// ABOUTME: Relays browser events through MediatR so tenant-aware governance still applies server-side.

using System.Security.Claims;
using Asp.Versioning;
using Explore.Application.DTOs.Analytics;
using Explore.Application.Features.PublicExperience.Requests.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/a/t")]
[ApiController]
public class AnalyticsRelayController : ControllerBase
{
    private readonly IMediator _mediator;

    public AnalyticsRelayController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(Extensions.RateLimitingExtensions.AnalyticsRelayPolicy)]
    [EndpointSummary("Relay Browser Analytics Event")]
    [EndpointDescription("Relays browser analytics through the server for tenants using relay transport mode.")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Relay([FromBody] RelayAnalyticsEventDto payload, CancellationToken cancellationToken)
    {
        var accepted = await _mediator.Send(new RelayAnalyticsEventCommand
        {
            AuthenticatedUserId = GetCurrentUserId(),
            Payload = payload
        }, cancellationToken);

        return accepted ? Accepted() : BadRequest();
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sid")?.Value;

        return Guid.TryParse(claim, out var userId) ? userId : null;
    }
}

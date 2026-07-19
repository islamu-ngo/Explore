// ABOUTME: Hosts the authenticated server-private endpoint that exchanges a verified PDS session for a platform JWT.
// ABOUTME: Excludes credential-bearing contracts from OpenAPI and disables response caching.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Authentication;
using Explore.API.Hateoas;
using Explore.API.Models;
using Explore.Application.Constants;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.Authentication.Atproto.Models;
using Explore.Application.Features.Authentication.Atproto.Requests.Commands;
using Explore.Application.Features.Authentication.Atproto.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/auth/atproto")]
[EndpointClassification(EndpointClass.Authenticated)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AtprotoSessionController(
    IMediator mediator,
    ITenantContext tenantContext) : ControllerBase
{
    [HttpPost("session", Name = RouteNames.BootstrapAtprotoSession)]
    [Authorize(AuthenticationSchemes = ApiAuthenticationSchemeNames.AtprotoBootstrap)]
    [EnableRateLimiting("write")]
    [RequestSizeLimit(160 * 1024)]
    [EndpointSummary("Bootstrap ATProto platform session")]
    [EndpointDescription("Server-private BFF bridge; excluded from public API discovery and generated clients.")]
    [ProducesResponseType<BffAtprotoSessionBridgeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<BffAtprotoSessionBridgeResponse>> BootstrapSession(
        [FromBody] BffAtprotoSessionBridgeRequest request,
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var sessionPayload = JsonSerializer.SerializeToUtf8Bytes(request.OAuthSession);
        var result = await mediator.Send(new BootstrapAtprotoSessionCommand(
            request.ExpectedDid,
            request.ExpectedPdsUri,
            request.OAuthClientKeyId,
            sessionPayload), cancellationToken);

        if (result.Success && result.Token is { } token && result.ExpiresAt is { } expiresAt)
        {
            return Ok(new BffAtprotoSessionBridgeResponse(result.UserId!.Value, request.ExpectedDid, token, expiresAt));
        }

        return result.FailureCode switch
        {
            "invalid_request" => ProblemResponse(StatusCodes.Status400BadRequest, "Invalid ATProto session request"),
            "account_not_linked" => ProblemResponse(StatusCodes.Status403Forbidden, "ATProto account is not linked"),
            "linked_identity_incomplete" or "identity_conflict" =>
                ProblemResponse(StatusCodes.Status409Conflict, "ATProto identity conflict"),
            "invalid_session" or "session_binding_mismatch" or "pds_identity_mismatch" =>
                ProblemResponse(StatusCodes.Status401Unauthorized, "ATProto session verification failed"),
            _ => ProblemResponse(StatusCodes.Status502BadGateway, "ATProto provider verification failed")
        };
    }

    [HttpGet("session/current", Name = RouteNames.GetCurrentAtprotoSession)]
    [Authorize(AuthenticationSchemes = ApiAuthenticationSchemeNames.AtprotoSession)]
    [EnableRateLimiting("authenticated")]
    [EndpointSummary("Get current ATProto OAuth session")]
    [EndpointDescription("Server-private BFF operation; requires the session bearer and a single-use bridge assertion.")]
    [ProducesResponseType<AtprotoCurrentSessionBridgeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AtprotoCurrentSessionBridgeResponse>> GetCurrentSession(
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (GetCurrentIdentity() is not { } identity)
        {
            return ProblemResponse(StatusCodes.Status401Unauthorized, "ATProto session authentication failed");
        }

        var session = await mediator.Send(
            new GetCurrentAtprotoOAuthSessionQuery(identity),
            cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return ProblemResponse(StatusCodes.Status404NotFound, "ATProto session not found");
        }

        using var document = JsonDocument.Parse(session.OAuthSessionPayload);
        return Ok(new AtprotoCurrentSessionBridgeResponse(
            session.Did,
            session.ExpectedPdsUri.AbsoluteUri,
            session.OAuthClientKeyId,
            document.RootElement.Clone()));
    }

    [HttpPost("session/current", Name = RouteNames.RefreshCurrentAtprotoSession)]
    [Authorize(AuthenticationSchemes = ApiAuthenticationSchemeNames.AtprotoSession)]
    [EnableRateLimiting("write")]
    [EndpointSummary("Refresh current ATProto OAuth session")]
    [EndpointDescription("Server-private BFF operation; persists rotated OAuth state before returning a replacement session token.")]
    [ProducesResponseType<BffAtprotoSessionBridgeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BffAtprotoSessionBridgeResponse>> RefreshCurrentSession(
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (GetCurrentIdentity() is not { } identity)
        {
            return ProblemResponse(StatusCodes.Status401Unauthorized, "ATProto session authentication failed");
        }

        var result = await mediator.Send(
            new RefreshAtprotoSessionCommand(identity),
            cancellationToken).ConfigureAwait(false);
        return result.Success && result.Token is { } token && result.ExpiresAt is { } expiresAt
            ? Ok(new BffAtprotoSessionBridgeResponse(identity.UserId, identity.Did, token, expiresAt))
            : ProblemResponse(StatusCodes.Status401Unauthorized, "ATProto reauthentication required");
    }

    [HttpDelete("session/current", Name = RouteNames.DeleteCurrentAtprotoSession)]
    [Authorize(AuthenticationSchemes = ApiAuthenticationSchemeNames.AtprotoSession)]
    [EnableRateLimiting("write")]
    [EndpointSummary("Delete current ATProto OAuth session")]
    [EndpointDescription("Server-private idempotent BFF operation; requires the session bearer and a single-use bridge assertion.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteCurrentSession(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (GetCurrentIdentity() is not { } identity)
        {
            return ProblemResponse(StatusCodes.Status401Unauthorized, "ATProto session authentication failed");
        }

        await mediator.Send(
            new RevokeAtprotoSessionCommand(identity),
            cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    private AtprotoCurrentSessionIdentity? GetCurrentIdentity()
    {
        return Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId)
               && User.FindFirstValue(AtprotoJwtOptions.DidClaim) is { } did
            ? new AtprotoCurrentSessionIdentity(tenantContext.TenantId, userId, did)
            : null;
    }

    private ObjectResult ProblemResponse(int status, string title) => Problem(
        statusCode: status,
        title: title,
        detail: "The server-private ATProto session bridge could not complete the request.");
}

public sealed record AtprotoCurrentSessionBridgeResponse(
    string Did,
    string ExpectedPdsUri,
    string OAuthClientKeyId,
    JsonElement OAuthSession);

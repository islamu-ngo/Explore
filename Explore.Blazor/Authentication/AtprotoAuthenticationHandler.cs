// ABOUTME: Stub authentication handler for AT Protocol OAuth (DPoP + PAR).
// ABOUTME: Returns NoResult until FishyFlip integration is implemented in a later phase.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Explore.Blazor.Authentication;

/// <summary>
/// Authentication handler for AT Protocol DID-based authentication.
/// This is a placeholder that registers the scheme so the login UI can enumerate it.
/// Full implementation (FishyFlip OAuth with DPoP, PAR, handle→DID→PDS discovery)
/// will be added in a later implementation phase.
/// </summary>
public class AtprotoAuthenticationHandler : AuthenticationHandler<AtprotoAuthenticationOptions>
{
    public AtprotoAuthenticationHandler(
        IOptionsMonitor<AtprotoAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // ATProto auth uses a separate challenge/callback flow, not passive authentication.
        // Return NoResult so the cookie scheme remains the passive authenticator.
        return Task.FromResult(AuthenticateResult.NoResult());
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // TODO: Implement ATProto OAuth challenge flow:
        // 1. Read handle from properties (passed from login form)
        // 2. Resolve handle → DID → PDS → Authorization Server
        // 3. Send PAR request with DPoP proof
        // 4. Redirect user to Authorization Server

        Logger.LogWarning(
            "ATProto authentication challenge invoked but handler is not yet fully implemented. " +
            "Full FishyFlip integration is required.");

        Context.Response.StatusCode = StatusCodes.Status501NotImplemented;
        return Context.Response.WriteAsJsonAsync(new
        {
            error = "ATProto authentication is not yet fully implemented. " +
                    "This feature is under active development."
        });
    }
}

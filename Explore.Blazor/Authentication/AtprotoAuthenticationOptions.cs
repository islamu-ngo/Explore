// ABOUTME: Authentication options for AT Protocol OAuth (DPoP + PAR via FishyFlip).
// ABOUTME: Stores public URL and will hold ES256 key pair once FishyFlip integration is complete.

using Microsoft.AspNetCore.Authentication;

namespace Explore.Blazor.Authentication;

/// <summary>
/// Options for the AT Protocol authentication handler.
/// Full implementation with FishyFlip will populate ES256 keys and client metadata.
/// </summary>
public class AtprotoAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// The publicly accessible URL of this instance, used as the ATProto OAuth client_id.
    /// Must be reachable by ATProto authorization servers to fetch client metadata.
    /// </summary>
    public string PublicUrl { get; set; } = string.Empty;

    /// <summary>
    /// Callback path for the ATProto OAuth redirect. Default: /signin-atproto.
    /// </summary>
    public string CallbackPath { get; set; } = "/signin-atproto";
}

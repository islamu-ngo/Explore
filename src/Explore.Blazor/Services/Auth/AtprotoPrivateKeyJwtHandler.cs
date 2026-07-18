// ABOUTME: Adapts the BFF private OAuth key ring to the shared strict assertion transport policy.
// ABOUTME: Creates a fresh CarpaNet ES256 client assertion with the session-pinned kid per send.

using CarpaNet.OAuth.Crypto;
using Explore.Atproto.Transport;

namespace Explore.Blazor.Services.Auth;

public sealed class AtprotoPrivateKeyJwtHandler(
    AtprotoAuthorizationServerRegistry registry,
    AtprotoClientKeyProvider keyProvider,
    string clientId,
    string callbackUri,
    string requiredScope,
    string pinnedKeyId,
    HttpMessageHandler innerHandler)
    : AtprotoPrivateKeyJwtHandlerBase(
        registry,
        clientId,
        callbackUri,
        requiredScope,
        innerHandler)
{
    protected override async ValueTask<string> CreateAssertionAsync(
        string issuer,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var signingKey = keyProvider.CreateCarpaSigningKey(pinnedKeyId);
        return await ClientAssertion.CreateAsync(
            ClientId,
            issuer,
            signingKey,
            pinnedKeyId).ConfigureAwait(false);
    }
}

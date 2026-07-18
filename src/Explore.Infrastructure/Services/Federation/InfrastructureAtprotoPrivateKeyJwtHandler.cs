// ABOUTME: Adapts the instance secret-backed key ring to the shared strict OAuth assertion policy.
// ABOUTME: Produces a fresh issuer-bound CarpaNet assertion with the persisted session kid per send.

using CarpaNet.OAuth.Crypto;
using Explore.Atproto.Transport;

namespace Explore.Infrastructure.Services.Federation;

internal sealed class InfrastructureAtprotoPrivateKeyJwtHandler(
    AtprotoAuthorizationServerRegistry registry,
    InfrastructureAtprotoKeyRing keys,
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
        using var key = keys.CreateKey(pinnedKeyId);
        return await ClientAssertion.CreateAsync(
            ClientId,
            issuer,
            key,
            pinnedKeyId).ConfigureAwait(false);
    }
}

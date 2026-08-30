// ABOUTME: Guards generated browser contracts from ATProto credentials and private bridge operations.
// ABOUTME: Proves removed raw-token and direct-record mutation symbols stay out of client and serializer sources.

namespace Explore.Blazor.Client.Tests.Security;

public sealed class AtprotoCredentialIsolationTests
{
    private static readonly string[] ForbiddenBrowserSymbols =
    [
        "BffAtprotoSessionBridgeRequest",
        "BffAtprotoSessionBridgeResponse",
        "AtprotoCurrentSessionBridgeResponse",
        "AtprotoOAuthFlowSeed",
        "AtprotoOAuthSession",
        "AtprotoSessionBridge",
        "AtprotoSessionBootstrap",
        "BootstrapAtprotoSessionAsync",
        "GetCurrentAtprotoSessionAsync",
        "DeleteCurrentAtprotoSessionAsync",
        "/api/auth/atproto/session",
        "OAuthSession",
        "OAuthSessionData",
        "OAuthClientKeyId",
        "DPoP",
        "DpopPrivateKey",
        "PdsAccessToken",
        "PdsRefreshToken",
        "AccessJwt",
        "RefreshJwt",
        "PrivateJwk",
        "PrivateJwks",
        "ExpectedPdsUri",
        "SessionCiphertext",
        "CreateUserAuthenticationTokenDto",
        "UpdateUserAuthenticationTokenDto",
        "CreateUserAuthenticationTokenAsync",
        "UpdateUserAuthenticationTokenAsync",
        "CreateAtprotoRecordDto",
        "UpdateAtprotoRecordDto",
        "CreateAtprotoRecordEntryAsync",
        "UpdateAtprotoRecordEntryAsync",
        "DeleteAtprotoRecordEntryAsync"
    ];

    [Test]
    public async Task GeneratedClientPublicSurfaceExcludesAtprotoCredentialMembers()
    {
        var publicSurface = typeof(Explore.Blazor.Client.Clients.IEventApiClient).Assembly
            .GetTypes()
            .Where(type => string.Equals(type.Namespace, "Explore.Blazor.Client.Clients", StringComparison.Ordinal))
            .SelectMany(type => type.GetProperties().Select(property => $"{type.Name}.{property.Name}").Prepend(type.Name))
            .ToArray();

        foreach (var forbidden in ForbiddenBrowserSymbols)
        {
            await Assert.That(publicSurface.Any(member => member.Contains(forbidden, StringComparison.OrdinalIgnoreCase)))
                .IsFalse()
                .Because($"The generated public surface must not expose {forbidden}.");
        }
    }

}

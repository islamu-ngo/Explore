// ABOUTME: Carries server-private ATProto verification inputs, verified identity state, and session results.
// ABOUTME: Uses opaque bytes so CarpaNet credentials never leak into public DTOs or outer clients.

namespace Explore.Application.Features.Authentication.Atproto.Models;

public sealed record AtprotoOAuthVerificationInput(
    string ExpectedDid,
    Uri ExpectedPdsUri,
    string OAuthClientKeyId,
    byte[] OAuthSessionPayload);

public sealed record AtprotoVerifiedOAuthSession(
    string Did,
    string Handle,
    Uri PdsUri,
    string OAuthClientKeyId,
    byte[] OAuthSessionPayload);

public sealed record AtprotoOAuthVerificationResult(
    AtprotoVerifiedOAuthSession? Session,
    string? FailureCode)
{
    public static AtprotoOAuthVerificationResult Verified(AtprotoVerifiedOAuthSession session) => new(session, null);
    public static AtprotoOAuthVerificationResult Failed(string code) => new(null, code);
}

public sealed record AtprotoIssuedSessionToken(string Token, DateTimeOffset ExpiresAt);

public sealed record AtprotoOAuthRefreshResult(bool Success, string FailureCode)
{
    public static AtprotoOAuthRefreshResult Refreshed() => new(true, string.Empty);

    public static AtprotoOAuthRefreshResult ReauthenticationRequired() =>
        new(false, "reauthentication_required");
}

public sealed record AtprotoSessionRefreshResult(
    bool Success,
    string FailureCode,
    string? Token = null,
    DateTimeOffset? ExpiresAt = null)
{
    public static AtprotoSessionRefreshResult Failed(string code) => new(false, code);

    public static AtprotoSessionRefreshResult Succeeded(AtprotoIssuedSessionToken token) =>
        new(true, string.Empty, token.Token, token.ExpiresAt);
}

public sealed record AtprotoSessionBootstrapResult(
    bool Success,
    string FailureCode,
    Guid? UserId = null,
    string? Token = null,
    DateTimeOffset? ExpiresAt = null)
{
    public static AtprotoSessionBootstrapResult Failed(string code) => new(false, code);

    public static AtprotoSessionBootstrapResult Succeeded(Guid userId, AtprotoIssuedSessionToken token) =>
        new(true, string.Empty, userId, token.Token, token.ExpiresAt);
}

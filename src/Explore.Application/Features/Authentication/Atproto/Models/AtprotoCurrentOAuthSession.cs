// ABOUTME: Carries the authenticated tenant/user/DID binding and opaque current OAuth session payload.
// ABOUTME: Keeps CarpaNet storage types outside Application while supporting the private BFF adapter.

namespace Explore.Application.Features.Authentication.Atproto.Models;

public sealed record AtprotoCurrentSessionIdentity(
    Guid TenantId,
    Guid UserId,
    string Did);

public sealed record AtprotoCurrentOAuthSession
{
    public AtprotoCurrentOAuthSession(
        string Did,
        Uri ExpectedPdsUri,
        string OAuthClientKeyId,
        ReadOnlyMemory<byte> OAuthSessionPayload)
    {
        this.Did = Did;
        this.ExpectedPdsUri = ExpectedPdsUri;
        this.OAuthClientKeyId = OAuthClientKeyId;
        this.OAuthSessionPayload = OAuthSessionPayload.ToArray();
    }

    public string Did { get; }
    public Uri ExpectedPdsUri { get; }
    public string OAuthClientKeyId { get; }
    public ReadOnlyMemory<byte> OAuthSessionPayload { get; }
}

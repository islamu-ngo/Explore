// ABOUTME: Carries the authenticated tenant/user/DID binding and opaque current OAuth session payload.
// ABOUTME: Keeps CarpaNet storage types outside Application while supporting the private BFF adapter.

namespace Explore.Application.Features.Authentication.Atproto.Models;

public sealed record AtprotoCurrentSessionIdentity(
    Guid TenantId,
    Guid UserId,
    string Did);

public sealed record AtprotoCurrentOAuthSession(
    string Did,
    Uri ExpectedPdsUri,
    string OAuthClientKeyId,
    byte[] OAuthSessionPayload);

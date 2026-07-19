// ABOUTME: Carries one protected ATProto OAuth flow binding across CarpaNet state consumption and session storage.
// ABOUTME: Prevents issuer, tenant, DID, PDS, origin, return-path, and signing-key substitution during callback.

namespace Explore.Blazor.Services.Auth;

public sealed record AtprotoOAuthFlowSeed(
    string ExpectedDid,
    Uri ExpectedPdsUri,
    Guid TenantId,
    string TenantSlug,
    Uri Origin,
    string ReturnPath,
    string OAuthClientKeyId);

public sealed record AtprotoOAuthFlowBinding(
    AtprotoOAuthFlowSeed Seed,
    Uri Issuer);

public sealed record AtprotoBffSessionResult(
    Guid UserId,
    string Did,
    string AccessToken,
    DateTimeOffset ExpiresAt);

public sealed class AtprotoOAuthFlowContext
{
    public AtprotoOAuthFlowBinding? Binding { get; private set; }

    public AtprotoBffSessionResult? SessionResult { get; private set; }

    public void BindConsumedState(AtprotoOAuthFlowBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        if (Binding is not null)
        {
            throw new InvalidOperationException("ATProto OAuth state was already consumed in this request.");
        }

        Binding = binding;
    }

    public void CaptureSession(AtprotoBffSessionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (Binding is null || SessionResult is not null)
        {
            throw new InvalidOperationException("ATProto OAuth session callback ordering is invalid.");
        }

        SessionResult = result;
    }
}

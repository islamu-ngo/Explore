// ABOUTME: Application-layer contract to signal the API's JWT authority must reload its OIDC metadata.
// ABOUTME: Implementation lives in Explore.API; handlers call this after onboarding/auth-config mutations.

namespace Explore.Application.Contracts.Services;

public interface IJwtAuthorityRefreshNotifier
{
    Task ReloadAsync(CancellationToken ct = default);
}

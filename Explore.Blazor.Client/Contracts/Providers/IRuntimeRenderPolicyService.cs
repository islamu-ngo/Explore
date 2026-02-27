// ABOUTME: Contract for runtime render policy resolution by route.
// ABOUTME: Keeps render strategy decisions testable and host-agnostic.

using Explore.Blazor.Client.Services;

namespace Explore.Blazor.Client.Contracts.Providers;

public interface IRuntimeRenderPolicyService
{
    Task<RuntimeRenderPolicyDecision> ResolveForPathAsync(string? rawPath, CancellationToken cancellationToken = default);
}

// ABOUTME: Benchmark-only authorization provider that removes external Cerbos latency from API measurements.
// ABOUTME: Allows benchmark scenarios to focus on API pipeline, serialization, caching, and data access costs.

using Explore.Application.Contracts.Infrastructure;

namespace Event.Benchmarks.Api;

internal sealed class ApiBenchmarkAuthorizationProvider : IAuthorizationProvider
{
    public static readonly ApiBenchmarkAuthorizationProvider AllowAll = new();

    public Task<AuthorizationDecision> AuthorizeAsync(
        AuthorizationRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AuthorizationDecision.Allow(AuthorizationProviderMetadata.Local));

    public Task<IReadOnlyList<AuthorizationDecision>> AuthorizeBatchAsync(
        IReadOnlyList<AuthorizationRequest> requests,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AuthorizationDecision>>(requests
            .Select(_ => AuthorizationDecision.Allow(AuthorizationProviderMetadata.Local))
            .ToArray());
}

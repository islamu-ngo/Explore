// ABOUTME: Benchmark-only authorization provider that removes external Cerbos latency from API measurements.
// ABOUTME: Allows benchmark scenarios to focus on API pipeline, serialization, caching, and data access costs.

using Explore.Application.Contracts.Infrastructure;

namespace Event.Benchmarks.Api;

internal sealed class ApiBenchmarkAuthorizationProvider : IAuthorizationProvider
{
    public static readonly ApiBenchmarkAuthorizationProvider AllowAll = new();

    public Task<bool> IsAllowedAsync(
        string resourceKind,
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<bool>> IsAllowedBatchAsync(
        IReadOnlyList<AuthorizationRequest> checks,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<bool> results = checks.Select(_ => true).ToList();
        return Task.FromResult(results);
    }

    public Task<bool> CheckSettingAccessAsync(
        string settingKey,
        string action,
        Guid? tenantId = null,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}

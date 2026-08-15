// ABOUTME: Stub IAuthorizationProvider for integration tests.
// Configurable to allow-all or deny-all for testing endpoint authorization behavior.

using Explore.Application.Contracts.Infrastructure;

namespace Event.Api.IntegrationTests.Fixtures;

/// <summary>
/// Stub authorization provider for integration tests.
/// Allows configuring per-action allow/deny behavior.
/// </summary>
public class StubAuthorizationProvider : IAuthorizationProvider
{
    /// <summary>
    /// When true, all authorization checks return allowed. When false, all return denied.
    /// </summary>
    public bool AllowAll { get; set; } = true;

    /// <summary>
    /// Optional predicate for fine-grained control over which checks pass.
    /// When set, overrides AllowAll for each individual check.
    /// </summary>
    public Func<AuthorizationRequest, bool>? CheckPredicate { get; set; }

    public async Task<AuthorizationDecision> AuthorizeAsync(
        AuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        var allowed = (await IsAllowedBatchAsync([request], cancellationToken))[0];
        return allowed
            ? AuthorizationDecision.Allow(AuthorizationProviderMetadata.Local)
            : AuthorizationDecision.Deny(AuthorizationProviderMetadata.Local);
    }

    public async Task<IReadOnlyList<AuthorizationDecision>> AuthorizeBatchAsync(
        IReadOnlyList<AuthorizationRequest> requests,
        CancellationToken cancellationToken = default) =>
        (await IsAllowedBatchAsync(requests, cancellationToken))
            .Select(allowed => allowed
                ? AuthorizationDecision.Allow(AuthorizationProviderMetadata.Local)
                : AuthorizationDecision.Deny(AuthorizationProviderMetadata.Local))
            .ToArray();

    public Task<bool> IsAllowedAsync(string resourceKind, string resourceId, string action,
        IDictionary<string, object>? resourceAttributes = null, CancellationToken cancellationToken = default)
    {
        if (CheckPredicate is not null)
        {
            IReadOnlyDictionary<string, object>? attrs = resourceAttributes is not null
                ? new Dictionary<string, object>(resourceAttributes)
                : null;
            var check = new AuthorizationRequest(resourceKind, resourceId, action, attrs);
            return Task.FromResult(CheckPredicate(check));
        }

        return Task.FromResult(AllowAll);
    }

    public Task<IReadOnlyList<bool>> IsAllowedBatchAsync(IReadOnlyList<AuthorizationRequest> checks,
        CancellationToken cancellationToken = default)
    {
        if (CheckPredicate is not null)
        {
            IReadOnlyList<bool> results = checks.Select(c => CheckPredicate(c)).ToList();
            return Task.FromResult(results);
        }

        IReadOnlyList<bool> batchResults = checks.Select(_ => AllowAll).ToList();
        return Task.FromResult(batchResults);
    }

    public Task<bool> CheckSettingAccessAsync(string settingKey, string action,
        Guid? tenantId = null, Guid? organizationId = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AllowAll);
    }
}

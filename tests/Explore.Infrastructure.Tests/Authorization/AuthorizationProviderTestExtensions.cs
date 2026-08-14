// ABOUTME: Test-only helpers for old authorization assertions during typed-port cutover.
// ABOUTME: Keeps production provider contracts free of bool/string compatibility methods.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;

namespace Explore.Application.Contracts.Infrastructure;

internal static class AuthorizationProviderTestExtensions
{
    public static async Task<bool> IsAllowedAsync(
        this IAuthorizationProvider provider,
        string resourceKind,
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes = null,
        CancellationToken cancellationToken = default)
    {
        var decision = await provider.AuthorizeAsync(
            new AuthorizationRequest(resourceKind, resourceId, action, resourceAttributes is null ? null : new Dictionary<string, object>(resourceAttributes)),
            cancellationToken);
        return decision.IsAllowed;
    }

    public static async Task<bool> IsAllowedWithFactsAsync(
        this IAuthorizationProvider provider,
        string resourceKind,
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken,
        IAuthorizationFacts? facts)
    {
        var decision = await provider.AuthorizeAsync(
            new AuthorizationRequest(
                resourceKind,
                resourceId,
                action,
                resourceAttributes is null ? null : new Dictionary<string, object>(resourceAttributes),
                facts: facts),
            cancellationToken);
        return decision.IsAllowed;
    }

    public static async Task<IReadOnlyList<bool>> IsAllowedBatchAsync(
        this IAuthorizationProvider provider,
        IReadOnlyList<AuthorizationRequest> requests,
        CancellationToken cancellationToken = default) =>
        (await provider.AuthorizeBatchAsync(requests, cancellationToken))
            .Select(decision => decision.IsAllowed)
            .ToArray();

    public static async Task<bool> CheckSettingAccessAsync(
        this IAuthorizationProvider provider,
        string settingKey,
        string action,
        Guid? tenantId = null,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        var attributes = new Dictionary<string, object> { ["settingKey"] = settingKey };
        var resourceKind = ResourceKinds.InstanceSetting;

        if (organizationId is { } organizationScope)
        {
            resourceKind = ResourceKinds.Organization;
            attributes["organizationId"] = organizationScope.ToString("D");
        }
        else if (tenantId is { } tenantScope)
        {
            resourceKind = ResourceKinds.TenantSetting;
            attributes["tenantId"] = tenantScope.ToString("D");
        }

        var decision = await provider.AuthorizeAsync(
            new AuthorizationRequest(resourceKind, settingKey, action, attributes),
            cancellationToken);
        return decision.IsAllowed;
    }
}

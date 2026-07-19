// ABOUTME: Bridges the singleton Jetstream subscriber to fresh scoped governance and fenced persistence operations.
// ABOUTME: Prevents concurrent BackgroundService work from sharing one pooled EF Core DbContext.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Services.Federation;
using Microsoft.Extensions.DependencyInjection;

namespace Explore.Infrastructure.Services.Federation;

public interface IAtprotoJetstreamRuntimeStore
{
    Task<IReadOnlyList<Guid>> ResolveEnabledTenantIdsAsync(CancellationToken cancellationToken);
    Task<AtprotoJetstreamClaim?> TryClaimAsync(
        string service,
        string owner,
        DateTime claimedAt,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);
    Task<bool> TryRenewAsync(
        AtprotoJetstreamClaim claim,
        DateTime observedAt,
        DateTime leaseExpiresAt,
        CancellationToken cancellationToken);
    Task<bool> TryApplyAndAdvanceAsync(
        AtprotoJetstreamApplyRequest request,
        CancellationToken cancellationToken);
}

internal sealed class AtprotoJetstreamRuntimeStore(IServiceScopeFactory scopeFactory)
    : IAtprotoJetstreamRuntimeStore
{
    public async Task<IReadOnlyList<Guid>> ResolveEnabledTenantIdsAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider
            .GetRequiredService<AtprotoJetstreamTenantPresentationResolver>()
            .ResolveEnabledTenantIdsAsync(cancellationToken);
    }

    public async Task<AtprotoJetstreamClaim?> TryClaimAsync(
        string service,
        string owner,
        DateTime claimedAt,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IAtprotoJetstreamRepository>()
            .TryClaimAsync(service, owner, claimedAt, leaseDuration, cancellationToken);
    }

    public async Task<bool> TryRenewAsync(
        AtprotoJetstreamClaim claim,
        DateTime observedAt,
        DateTime leaseExpiresAt,
        CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IAtprotoJetstreamRepository>()
            .TryRenewAsync(claim, observedAt, leaseExpiresAt, cancellationToken);
    }

    public async Task<bool> TryApplyAndAdvanceAsync(
        AtprotoJetstreamApplyRequest request,
        CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        bool applied = await scope.ServiceProvider.GetRequiredService<IAtprotoJetstreamRepository>()
            .TryApplyAndAdvanceAsync(request, cancellationToken);
        if (applied
            && AffectsEventDiscovery(request)
            && scope.ServiceProvider.GetService<IAtprotoDiscoveryCacheInvalidator>() is { } cacheInvalidator)
        {
            await cacheInvalidator.InvalidateAsync(cancellationToken);
        }

        return applied;
    }

    private static bool AffectsEventDiscovery(AtprotoJetstreamApplyRequest request) =>
        request.EventProjection is not null
        || request.EventProjectionInvalidation is not null
        || string.Equals(
            request.Record?.Collection,
            AtprotoJetstreamConstants.EventCollection,
            StringComparison.Ordinal);
}

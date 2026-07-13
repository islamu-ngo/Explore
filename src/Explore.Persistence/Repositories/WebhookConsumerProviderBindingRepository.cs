// ABOUTME: EF Core repository for normalized webhook provider-binding identities.
// ABOUTME: Uses tenant-scoped queries and dual version/fence predicates for authority changes.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class WebhookConsumerProviderBindingRepository(ExploreDbContext dbContext)
    : IWebhookConsumerProviderBindingRepository
{
    public async Task<WebhookConsumerProviderBinding> CreateAsync(
        WebhookConsumerProviderBinding binding,
        CancellationToken cancellationToken)
    {
        await dbContext.WebhookConsumerProviderBindings.AddAsync(binding, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return binding;
    }

    public async Task<WebhookConsumerProviderBinding?> GetVerifiedByConsumerAsync(
        Guid tenantId,
        Guid webhookConsumerId,
        WebhookProviderKind providerKind,
        string providerEnvironment,
        CancellationToken cancellationToken)
    {
        var normalizedEnvironment = WebhookConsumerProviderBinding.NormalizeIdentity(
            providerEnvironment,
            nameof(providerEnvironment));

        return await BindingQuery()
            .SingleOrDefaultAsync(binding =>
                binding.TenantId == tenantId &&
                binding.WebhookConsumerId == webhookConsumerId &&
                binding.ProviderKindId == (int)providerKind &&
                binding.NormalizedEnvironment == normalizedEnvironment &&
                binding.VerificationStateId == (int)WebhookProviderBindingVerificationState.Verified &&
                binding.VerifiedTenantId == tenantId &&
                binding.VerifiedWebhookConsumerId == webhookConsumerId &&
                binding.IsEnabled,
                cancellationToken);
    }

    public async Task<WebhookConsumerProviderBinding?> GetVerifiedByProviderIdentityAsync(
        Guid tenantId,
        WebhookProviderKind providerKind,
        string providerEnvironment,
        string externalApplicationId,
        string applicationUid,
        CancellationToken cancellationToken)
    {
        var normalizedEnvironment = WebhookConsumerProviderBinding.NormalizeIdentity(
            providerEnvironment,
            nameof(providerEnvironment));
        var normalizedExternalApplicationId = WebhookConsumerProviderBinding.NormalizeIdentity(
            externalApplicationId,
            nameof(externalApplicationId));
        var normalizedApplicationUid = WebhookConsumerProviderBinding.NormalizeIdentity(
            applicationUid,
            nameof(applicationUid));

        return await BindingQuery()
            .SingleOrDefaultAsync(binding =>
                binding.TenantId == tenantId &&
                binding.ProviderKindId == (int)providerKind &&
                binding.NormalizedEnvironment == normalizedEnvironment &&
                binding.NormalizedExternalApplicationId == normalizedExternalApplicationId &&
                binding.NormalizedApplicationUid == normalizedApplicationUid &&
                binding.VerificationStateId == (int)WebhookProviderBindingVerificationState.Verified &&
                binding.VerifiedTenantId == tenantId &&
                binding.IsEnabled,
                cancellationToken);
    }

    public async Task<IReadOnlyList<WebhookConsumerProviderBinding>> GetVerifiedByConsumersAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> webhookConsumerIds,
        WebhookProviderKind providerKind,
        string providerEnvironment,
        CancellationToken cancellationToken)
    {
        if (webhookConsumerIds.Count == 0)
        {
            return [];
        }

        var normalizedEnvironment = WebhookConsumerProviderBinding.NormalizeIdentity(
            providerEnvironment,
            nameof(providerEnvironment));
        var distinctConsumerIds = webhookConsumerIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (distinctConsumerIds.Length == 0)
        {
            return [];
        }

        return await BindingQuery()
            .Where(binding =>
                binding.TenantId == tenantId &&
                distinctConsumerIds.Contains(binding.WebhookConsumerId) &&
                binding.ProviderKindId == (int)providerKind &&
                binding.NormalizedEnvironment == normalizedEnvironment &&
                binding.VerificationStateId == (int)WebhookProviderBindingVerificationState.Verified &&
                binding.VerifiedTenantId == tenantId &&
                binding.VerifiedWebhookConsumerId == binding.WebhookConsumerId &&
                binding.IsEnabled)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryVerifyAsync(
        Guid tenantId,
        Guid bindingId,
        long expectedConcurrencyVersion,
        long expectedVerificationFence,
        string externalApplicationId,
        DateTimeOffset verifiedAtUtc,
        CancellationToken cancellationToken)
    {
        var providerApplicationId = externalApplicationId.Trim();
        var normalizedExternalApplicationId = WebhookConsumerProviderBinding.NormalizeIdentity(
            externalApplicationId,
            nameof(externalApplicationId));

        var updated = await MutableBindingQuery()
            .Where(binding =>
                binding.TenantId == tenantId &&
                binding.Id == bindingId &&
                binding.ConcurrencyVersion == expectedConcurrencyVersion &&
                binding.VerificationFence == expectedVerificationFence &&
                (binding.VerificationStateId == (int)WebhookProviderBindingVerificationState.Pending ||
                 binding.VerificationStateId == (int)WebhookProviderBindingVerificationState.LegacyUnverified))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(binding => binding.ExternalApplicationId, providerApplicationId)
                .SetProperty(binding => binding.NormalizedExternalApplicationId, normalizedExternalApplicationId)
                .SetProperty(binding => binding.VerifiedTenantId, tenantId)
                .SetProperty(binding => binding.VerifiedWebhookConsumerId, binding => binding.WebhookConsumerId)
                .SetProperty(binding => binding.VerifiedAtUtc, verifiedAtUtc)
                .SetProperty(binding => binding.VerificationStateId, (int)WebhookProviderBindingVerificationState.Verified)
                .SetProperty(binding => binding.IsEnabled, true)
                .SetProperty(binding => binding.ConcurrencyVersion, binding => binding.ConcurrencyVersion + 1)
                .SetProperty(binding => binding.VerificationFence, binding => binding.VerificationFence + 1)
                .SetProperty(binding => binding.UpdatedAt, verifiedAtUtc.UtcDateTime),
                cancellationToken);

        return updated == 1;
    }

    public async Task<bool> TryDisableAsync(
        Guid tenantId,
        Guid bindingId,
        long expectedConcurrencyVersion,
        long expectedVerificationFence,
        DateTimeOffset disabledAtUtc,
        CancellationToken cancellationToken)
    {
        var updated = await MutableBindingQuery()
            .Where(binding =>
                binding.TenantId == tenantId &&
                binding.Id == bindingId &&
                binding.ConcurrencyVersion == expectedConcurrencyVersion &&
                binding.VerificationFence == expectedVerificationFence &&
                binding.IsEnabled)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(binding => binding.IsEnabled, false)
                .SetProperty(binding => binding.ConcurrencyVersion, binding => binding.ConcurrencyVersion + 1)
                .SetProperty(binding => binding.VerificationFence, binding => binding.VerificationFence + 1)
                .SetProperty(binding => binding.UpdatedAt, disabledAtUtc.UtcDateTime),
                cancellationToken);

        return updated == 1;
    }

    public async Task<bool> TryRebindAsync(
        Guid tenantId,
        Guid bindingId,
        long expectedConcurrencyVersion,
        long expectedVerificationFence,
        string externalApplicationId,
        DateTimeOffset verifiedAtUtc,
        CancellationToken cancellationToken)
    {
        var providerApplicationId = externalApplicationId.Trim();
        var normalizedExternalApplicationId = WebhookConsumerProviderBinding.NormalizeIdentity(
            externalApplicationId,
            nameof(externalApplicationId));

        var updated = await MutableBindingQuery()
            .Where(binding =>
                binding.TenantId == tenantId &&
                binding.Id == bindingId &&
                binding.ConcurrencyVersion == expectedConcurrencyVersion &&
                binding.VerificationFence == expectedVerificationFence &&
                binding.VerificationStateId == (int)WebhookProviderBindingVerificationState.Verified)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(binding => binding.ExternalApplicationId, providerApplicationId)
                .SetProperty(binding => binding.NormalizedExternalApplicationId, normalizedExternalApplicationId)
                .SetProperty(binding => binding.VerifiedTenantId, tenantId)
                .SetProperty(binding => binding.VerifiedWebhookConsumerId, binding => binding.WebhookConsumerId)
                .SetProperty(binding => binding.VerifiedAtUtc, verifiedAtUtc)
                .SetProperty(binding => binding.IsEnabled, true)
                .SetProperty(binding => binding.ConcurrencyVersion, binding => binding.ConcurrencyVersion + 1)
                .SetProperty(binding => binding.VerificationFence, binding => binding.VerificationFence + 1)
                .SetProperty(binding => binding.UpdatedAt, verifiedAtUtc.UtcDateTime),
                cancellationToken);

        return updated == 1;
    }

    private IQueryable<WebhookConsumerProviderBinding> BindingQuery() =>
        MutableBindingQuery().AsNoTracking();

    private IQueryable<WebhookConsumerProviderBinding> MutableBindingQuery() =>
        dbContext.WebhookConsumerProviderBindings
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation);
}

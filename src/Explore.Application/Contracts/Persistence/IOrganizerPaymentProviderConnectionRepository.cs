// ABOUTME: Entity-first persistence contract for actor-bound organizer payment connections.
// ABOUTME: Defines scoped active and historical external-account lookups without DTO or IQueryable leakage.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IOrganizerPaymentProviderConnectionRepository
{
    Task<OrganizerPaymentProviderConnection?> GetActiveByScopeAsync(
        Guid tenantId,
        Guid organizerActorId,
        string providerCode,
        string connectPlatformId,
        CancellationToken cancellationToken);

    Task<OrganizerPaymentProviderConnection?> GetHistoricalByExternalAccountAsync(
        string providerCode,
        string connectPlatformId,
        string externalAccountId,
        CancellationToken cancellationToken);

    Task<OrganizerPaymentProviderConnection?> GetByTenantAndIdForUpdateAsync(
        Guid tenantId,
        Guid connectionId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OrganizerPaymentProviderConnection>> ListByTenantAndActorAsync(
        Guid tenantId,
        Guid organizerActorId,
        CancellationToken cancellationToken);

    Task CreateAsync(OrganizerPaymentProviderConnection connection, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

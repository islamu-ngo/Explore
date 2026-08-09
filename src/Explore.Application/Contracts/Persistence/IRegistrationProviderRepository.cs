// ABOUTME: Entity-first repository contract for persisted registration-provider connections and bindings.
// ABOUTME: Supports next-wave capability resolution without exposing DTO projections from Persistence.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IRegistrationProviderRepository
{
    Task<RegistrationProviderConnection?> GetConnectionAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken);

    Task<RegistrationProviderBinding?> GetBindingAsync(Guid tenantId, Guid bindingId, CancellationToken cancellationToken);

    Task<RegistrationProviderBinding?> GetBindingForCallbackAsync(Guid bindingId, CancellationToken cancellationToken);

    Task<bool> HasSubmissionForBindingAsync(Guid tenantId, Guid bindingId, CancellationToken cancellationToken);

    Task AddConnectionAsync(RegistrationProviderConnection connection, CancellationToken cancellationToken);

    Task AddBindingAsync(RegistrationProviderBinding binding, CancellationToken cancellationToken);

    Task AddSchemaRevisionAsync(RegistrationProviderSchemaRevision revision, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

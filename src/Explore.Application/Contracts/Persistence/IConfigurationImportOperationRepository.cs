// ABOUTME: Persists append-only configuration import receipts behind trusted target coordinates.
// ABOUTME: Returns Domain operations and never exposes protected snapshot bytes or source authority.

namespace Explore.Application.Contracts.Persistence;

using Explore.Domain;

public interface IConfigurationImportOperationRepository
{
    Task AddAsync(
        ConfigurationImportOperation operation,
        CancellationToken cancellationToken);

    Task<ConfigurationImportOperation?> GetByIdAsync(
        Guid operationId,
        string targetAuthorityKey,
        CancellationToken cancellationToken);

    Task<ConfigurationImportOperation?> GetByIdForEffectAsync(
        Guid operationId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ConfigurationImportOperation>> ListAsync(
        string targetAuthorityKey,
        int maximumCount,
        CancellationToken cancellationToken);
}

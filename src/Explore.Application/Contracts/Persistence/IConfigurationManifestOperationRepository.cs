// ABOUTME: Append-only persistence contracts for configuration-manifest operation and tenant-result evidence.
// ABOUTME: Separates transaction-bound outcomes from isolated post-rollback failure recording.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IConfigurationManifestOperationRepository
{
    Task<ConfigurationManifestOperation> CreateAsync(
        ConfigurationManifestOperation operation,
        IReadOnlyCollection<ConfigurationManifestTenantResult> tenantResults,
        CancellationToken cancellationToken);

    Task<ConfigurationManifestOperation?> GetLatestByDigestAsync(
        string digest,
        CancellationToken cancellationToken);

    Task<ConfigurationManifestOperation?> GetByIdAsync(
        Guid operationId,
        CancellationToken cancellationToken);

    Task<ConfigurationManifestOperation?> GetLatestAppliedBootstrapAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ConfigurationManifestTenantResult>> GetResultsByOperationIdAsync(
        Guid operationId,
        CancellationToken cancellationToken);

    Task<ConfigurationManifestTenantResult?> GetCurrentTenantResultAsync(
        Guid operationId,
        CancellationToken cancellationToken);
}

public interface IConfigurationManifestFailureRecorder
{
    Task RecordAsync(
        ConfigurationManifestOperation failedOperation,
        CancellationToken cancellationToken);
}

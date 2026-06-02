// ABOUTME: Application contract for bounded storage reconciliation passes.
// ABOUTME: Reports missing/orphaned storage state before policy-controlled quarantine or deletion.

using Explore.Application.Models.Storage;

namespace Explore.Application.Contracts.Services;

public interface IStorageReconciliationService
{
    Task<StorageReconciliationResult> ReconcileAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}

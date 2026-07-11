// ABOUTME: Application contract for deleting storage objects already marked delete-requested.
// ABOUTME: Lets moderation and reconciliation trigger provider-backed deletion without exposing provider keys.

using Explore.Application.Models.Storage;

namespace Explore.Application.Contracts.Services;

public interface IStorageObjectDeletionService
{
    Task<StorageObjectDeletionResult> DeleteRequestedForResourceAsync(
        Guid tenantId,
        string owningResourceKind,
        Guid owningResourceId,
        Guid? deletedBy,
        int limit,
        CancellationToken cancellationToken);
}

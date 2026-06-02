// ABOUTME: Optional storage provider capability for bounded backing-object inventory scans.
// ABOUTME: Reconciliation jobs use this to report and quarantine provider objects missing metadata.

using Explore.Application.Models.Storage;

namespace Explore.Application.Contracts.Infrastructure;

public interface IFileStorageInventoryProvider : IFileStorageProvider
{
    IAsyncEnumerable<FileStorageInventoryObject> ListObjectsAsync(
        int limit,
        CancellationToken cancellationToken);

    Task<FileStorageQuarantineResult> QuarantineAsync(
        FileStorageQuarantineInput input,
        CancellationToken cancellationToken);
}

// ABOUTME: Provider-neutral storage contract for local and optional remote file providers.
// ABOUTME: Application code depends on this abstraction instead of S3/presigned URL semantics.

using Explore.Application.Models.Storage;

namespace Explore.Application.Contracts.Infrastructure;

public interface IFileStorageProvider
{
    string Provider { get; }

    Task<FileStorageWriteResult> WriteAsync(FileStorageWriteInput input, CancellationToken cancellationToken);

    Task<FileStorageReadResult> OpenReadAsync(FileStorageReadInput input, CancellationToken cancellationToken);

    Task<FileStorageDeleteResult> DeleteAsync(FileStorageDeleteInput input, CancellationToken cancellationToken);

    Task<FileStorageProviderStatus> TestAsync(CancellationToken cancellationToken);
}

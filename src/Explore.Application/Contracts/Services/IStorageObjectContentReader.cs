// ABOUTME: Application service contract for opening storage object content by metadata ID.
// ABOUTME: Centralizes lifecycle and visibility checks before provider streams are exposed.

using Explore.Application.Models.Storage;

namespace Explore.Application.Contracts.Services;

public interface IStorageObjectContentReader
{
    Task<StorageObjectContentResult?> OpenAsync(
        Guid storageObjectId,
        bool publicImagesOnly,
        CancellationToken cancellationToken);
}

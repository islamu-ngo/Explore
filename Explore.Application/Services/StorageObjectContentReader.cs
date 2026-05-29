// ABOUTME: Metadata-first storage content reader for provider-neutral download endpoints.
// ABOUTME: Enforces lifecycle and visibility before opening server-owned provider object keys.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Models.Storage;
using Explore.Domain;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Services;

public sealed class StorageObjectContentReader : IStorageObjectContentReader
{
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IFileStorageProviderResolver _providerResolver;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<StorageObjectContentReader> _logger;

    public StorageObjectContentReader(
        IStorageObjectRepository storageObjectRepository,
        IFileStorageProviderResolver providerResolver,
        ICurrentUserService currentUserService,
        ILogger<StorageObjectContentReader> logger)
    {
        _storageObjectRepository = storageObjectRepository;
        _providerResolver = providerResolver;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<StorageObjectContentResult?> OpenAsync(
        Guid storageObjectId,
        bool publicImagesOnly,
        CancellationToken cancellationToken)
    {
        if (storageObjectId == Guid.Empty)
        {
            return null;
        }

        var storageObject = await _storageObjectRepository.GetById(storageObjectId);
        if (storageObject is null)
        {
            return null;
        }

        if (!CanRead(storageObject, publicImagesOnly))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(storageObject.ObjectKey))
        {
            _logger.LogWarning("Storage object {StorageObjectId} has no provider object key.", storageObjectId);
            return null;
        }

        try
        {
            var provider = _providerResolver.GetRequired(storageObject.Provider);
            var readResult = await provider.OpenReadAsync(
                new FileStorageReadInput(storageObject.ObjectKey, storageObject.ContentType),
                cancellationToken);

            return new StorageObjectContentResult(
                readResult.Content,
                readResult.ContentType,
                readResult.Length,
                readResult.LastModified,
                storageObject.Sha256Checksum);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "Storage provider object was not found for storage object {StorageObjectId}.", storageObjectId);
            return null;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Storage provider {Provider} is unavailable for storage object {StorageObjectId}.", storageObject.Provider, storageObjectId);
            return null;
        }
    }

    private bool CanRead(StorageObject storageObject, bool publicImagesOnly)
    {
        if (!string.Equals(storageObject.LifecycleState, StorageObjectLifecycleStates.Active, StringComparison.Ordinal))
        {
            return false;
        }

        if (publicImagesOnly)
        {
            return string.Equals(storageObject.Visibility, StorageObjectVisibilities.PublicImage, StringComparison.Ordinal);
        }

        if (string.Equals(storageObject.Visibility, StorageObjectVisibilities.PublicImage, StringComparison.Ordinal))
        {
            return true;
        }

        if (string.Equals(storageObject.Visibility, StorageObjectVisibilities.AuthenticatedTenant, StringComparison.Ordinal))
        {
            return _currentUserService.IsAuthenticated;
        }

        if (string.Equals(storageObject.Visibility, StorageObjectVisibilities.PrivateOwner, StringComparison.Ordinal))
        {
            return _currentUserService.UserId.HasValue && storageObject.CreatedBy == _currentUserService.UserId;
        }

        return false;
    }
}

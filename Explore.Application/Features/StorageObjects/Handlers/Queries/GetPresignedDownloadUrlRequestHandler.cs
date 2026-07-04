// ABOUTME: Query handler returning a pre-signed download URL for a storage object identified by ID.
// ABOUTME: Used for authenticated media downloads.
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Features.StorageObjects.Requests.Queries;
using Explore.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.StorageObjects.Handlers.Queries;

/// <summary>
/// Handler for getting a presigned download URL for a storage object by its ID.
/// </summary>
public class GetPresignedDownloadUrlRequestHandler : IRequestHandler<GetPresignedDownloadUrlRequest, PresignedDownloadUrlResponseDto?>
{
    private const int MinimumExpirationMinutes = 1;
    private const int MaximumExpirationMinutes = 60;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetPresignedDownloadUrlRequestHandler> _logger;

    public GetPresignedDownloadUrlRequestHandler(
        IStorageObjectRepository storageObjectRepository,
        IObjectStorageService objectStorageService,
        ICurrentUserService currentUserService,
        ILogger<GetPresignedDownloadUrlRequestHandler> logger)
    {
        _storageObjectRepository = storageObjectRepository;
        _objectStorageService = objectStorageService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<PresignedDownloadUrlResponseDto?> Handle(GetPresignedDownloadUrlRequest request, CancellationToken cancellationToken)
    {
        if (request.Id == Guid.Empty)
        {
            _logger.LogWarning("Rejected presigned download request with empty storage object ID.");
            return null;
        }

        if (request.ExpirationMinutes is < MinimumExpirationMinutes or > MaximumExpirationMinutes)
        {
            _logger.LogWarning(
                "Rejected presigned download request with invalid expiration. StorageObjectId={StorageObjectId}, ExpirationMinutes={ExpirationMinutes}",
                request.Id,
                request.ExpirationMinutes);
            return null;
        }

        var storageObject = await _storageObjectRepository.GetById(request.Id);

        if (storageObject == null)
        {
            _logger.LogWarning("Storage object metadata was not found for presigned download. StorageObjectId={StorageObjectId}", request.Id);
            return null;
        }

        if (!CanRead(storageObject))
        {
            _logger.LogWarning(
                "Rejected presigned download request for inaccessible storage object. StorageObjectId={StorageObjectId}, Visibility={Visibility}",
                request.Id,
                storageObject.Visibility);
            return null;
        }

        if (string.IsNullOrWhiteSpace(storageObject.ObjectKey))
        {
            _logger.LogWarning(
                "Storage object has no provider object key for presigned download. StorageObjectId={StorageObjectId}",
                request.Id);
            return null;
        }

        try
        {
            var presignedUrl = await _objectStorageService.GeneratePresignedDownloadUrl(
                storageObject.ObjectKey,
                request.ExpirationMinutes);

            return new PresignedDownloadUrlResponseDto
            {
                PresignedUrl = presignedUrl,
                ObjectKey = string.Empty,
                ExpiresInMinutes = request.ExpirationMinutes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Failed to generate presigned URL for storage object. StorageObjectId={StorageObjectId}, FailureType={FailureType}",
                request.Id,
                CategorizeSigningFailure(ex));
            return null;
        }
    }

    private bool CanRead(StorageObject storageObject)
    {
        if (!string.Equals(storageObject.LifecycleState, StorageObjectLifecycleStates.Active, StringComparison.Ordinal))
        {
            return false;
        }

        if (string.Equals(storageObject.Visibility, StorageObjectVisibilities.PublicImage, StringComparison.Ordinal))
        {
            return true;
        }

        if (string.Equals(storageObject.Visibility, StorageObjectVisibilities.AuthenticatedTenant, StringComparison.Ordinal))
        {
            return _currentUserService.IsAuthenticated;
        }

        return string.Equals(storageObject.Visibility, StorageObjectVisibilities.PrivateOwner, StringComparison.Ordinal) &&
               _currentUserService.UserId.HasValue &&
               storageObject.CreatedBy == _currentUserService.UserId;
    }

    private static string CategorizeSigningFailure(Exception exception) =>
        exception switch
        {
            TimeoutException => "timeout",
            InvalidOperationException => "provider_unavailable",
            IOException => "provider_io",
            ArgumentException => "invalid_provider_request",
            _ => "unknown"
        };
}

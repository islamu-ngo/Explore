using System;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Features.StorageObjects.Requests.Queries;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.StorageObjects.Handlers.Queries;

/// <summary>
/// Handler for getting a presigned download URL for a storage object by its ID.
/// </summary>
public class GetPresignedDownloadUrlRequestHandler : IRequestHandler<GetPresignedDownloadUrlRequest, PresignedDownloadUrlResponseDto?>
{
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<GetPresignedDownloadUrlRequestHandler> _logger;

    public GetPresignedDownloadUrlRequestHandler(
        IStorageObjectRepository storageObjectRepository,
        IObjectStorageService objectStorageService,
        ILogger<GetPresignedDownloadUrlRequestHandler> logger)
    {
        _storageObjectRepository = storageObjectRepository;
        _objectStorageService = objectStorageService;
        _logger = logger;
    }

    public async Task<PresignedDownloadUrlResponseDto?> Handle(GetPresignedDownloadUrlRequest request, CancellationToken cancellationToken)
    {
        var storageObject = await _storageObjectRepository.GetById(request.Id);

        if (storageObject == null)
        {
            _logger.LogWarning("Storage object not found: {Id}", request.Id);
            return null;
        }

        var objectKey = ExtractObjectKeyFromUri(storageObject.Uri);

        if (string.IsNullOrEmpty(objectKey))
        {
            _logger.LogWarning("Could not extract object key from URI: {Uri}", storageObject.Uri);
            return null;
        }

        try
        {
            var presignedUrl = await _objectStorageService.GeneratePresignedDownloadUrl(objectKey, request.ExpirationMinutes);

            return new PresignedDownloadUrlResponseDto
            {
                PresignedUrl = presignedUrl,
                ObjectKey = objectKey,
                ExpiresInMinutes = request.ExpirationMinutes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate presigned URL for storage object: {Id}", request.Id);
            return null;
        }
    }

    /// <summary>
    /// Extracts the object key from a full S3 URI or returns the key if it's already just a key.
    /// </summary>
    private static string? ExtractObjectKeyFromUri(string uri)
    {
        if (string.IsNullOrEmpty(uri))
            return null;

        // If it's already just a key (doesn't start with http), return as-is
        if (!uri.StartsWith("http://") && !uri.StartsWith("https://"))
            return uri;

        try
        {
            var uriObj = new Uri(uri);
            // The path starts with '/', so we trim it
            var path = uriObj.AbsolutePath.TrimStart('/');
            return string.IsNullOrEmpty(path) ? null : path;
        }
        catch
        {
            return null;
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Features.StorageObjects.Requests.Queries;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.StorageObjects.Handlers.Queries;

/// <summary>
/// Handler for getting a presigned download URL using an object key directly.
/// </summary>
public class GetPresignedDownloadUrlByKeyRequestHandler : IRequestHandler<GetPresignedDownloadUrlByKeyRequest, PresignedDownloadUrlResponseDto?>
{
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<GetPresignedDownloadUrlByKeyRequestHandler> _logger;

    public GetPresignedDownloadUrlByKeyRequestHandler(
        IObjectStorageService objectStorageService,
        ILogger<GetPresignedDownloadUrlByKeyRequestHandler> logger)
    {
        _objectStorageService = objectStorageService;
        _logger = logger;
    }

    public Task<PresignedDownloadUrlResponseDto?> Handle(GetPresignedDownloadUrlByKeyRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.ObjectKey))
        {
            _logger.LogWarning("Object key cannot be empty");
            return Task.FromResult<PresignedDownloadUrlResponseDto?>(null);
        }

        try
        {
            var presignedUrl = _objectStorageService.GeneratePresignedDownloadUrl(request.ObjectKey, request.ExpirationMinutes);

            var response = new PresignedDownloadUrlResponseDto
            {
                PresignedUrl = presignedUrl,
                ObjectKey = request.ObjectKey,
                ExpiresInMinutes = request.ExpirationMinutes
            };

            return Task.FromResult<PresignedDownloadUrlResponseDto?>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate presigned URL for object key: {ObjectKey}", request.ObjectKey);
            return Task.FromResult<PresignedDownloadUrlResponseDto?>(null);
        }
    }
}

// ABOUTME: Resolves a storage object by ID and streams its content for public image proxy.
// ABOUTME: Returns null if the storage object does not exist, enabling 404 at the controller level.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.StorageObjects.Requests.Queries;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.StorageObjects.Handlers.Queries;

public class GetPublicImageRequestHandler : IRequestHandler<GetPublicImageRequest, (Stream FileStream, string ContentType)?>
{
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<GetPublicImageRequestHandler> _logger;

    public GetPublicImageRequestHandler(
        IStorageObjectRepository storageObjectRepository,
        IObjectStorageService objectStorageService,
        ILogger<GetPublicImageRequestHandler> logger)
    {
        _storageObjectRepository = storageObjectRepository;
        _objectStorageService = objectStorageService;
        _logger = logger;
    }

    public async Task<(Stream FileStream, string ContentType)?> Handle(
        GetPublicImageRequest request, CancellationToken cancellationToken)
    {
        var storageObject = await _storageObjectRepository.GetById(request.StorageObjectId);
        if (storageObject is null)
        {
            _logger.LogWarning("Public image proxy: storage object {Id} not found", request.StorageObjectId);
            return null;
        }

        try
        {
            var (fileStream, contentType) = await _objectStorageService.GetFileStream(storageObject.Uri);
            return (fileStream, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Public image proxy: failed to retrieve file for storage object {Id}", request.StorageObjectId);
            return null;
        }
    }
}

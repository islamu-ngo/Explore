using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Actor;
using Explore.Application.Features.Actors.Requests.Queries;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Actors.Handlers.Queries;

public class GetActorDetailsRequestHandler : IRequestHandler<GetActorDetailsRequest, ActorDto>
{
    private readonly IActorRepository _actorRepository;
    private readonly IMapper _mapper;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<GetActorDetailsRequestHandler> _logger;

    public GetActorDetailsRequestHandler(
        IActorRepository actorRepository,
        IMapper mapper,
        IObjectStorageService objectStorageService,
        ILogger<GetActorDetailsRequestHandler> logger)
    {
        _actorRepository = actorRepository;
        _mapper = mapper;
        _objectStorageService = objectStorageService;
        _logger = logger;
    }

    public async Task<ActorDto> Handle(GetActorDetailsRequest request, CancellationToken cancellationToken)
    {
        var actor = await _actorRepository.GetActorWithDetails(request.Id);
        var dto = _mapper.Map<ActorDto>(actor);

        // Resolve presigned URL for profile picture
        if (dto != null)
        {
            dto.ProfilePictureUri = ResolveImageUrl(dto.ProfilePictureUri);
        }

        return dto;
    }

    /// <summary>
    /// Resolves an image object key to a presigned URL for viewing.
    /// </summary>
    private string? ResolveImageUrl(string? objectKeyOrUri)
    {
        if (string.IsNullOrEmpty(objectKeyOrUri))
            return null;

        try
        {
            // Check if it's already a full URL (legacy data)
            if (objectKeyOrUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                objectKeyOrUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                if (Uri.TryCreate(objectKeyOrUri, UriKind.Absolute, out var uri))
                {
                    var objectKey = uri.AbsolutePath.TrimStart('/');
                    return _objectStorageService.GeneratePresignedDownloadUrl(objectKey, 60);
                }
                return objectKeyOrUri;
            }

            // It's an object key - generate presigned URL
            return _objectStorageService.GeneratePresignedDownloadUrl(objectKeyOrUri, 60);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate presigned URL for object key: {ObjectKey}", objectKeyOrUri);
            return null;
        }
    }
}

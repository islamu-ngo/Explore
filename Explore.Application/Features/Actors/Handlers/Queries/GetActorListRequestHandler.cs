// ABOUTME: Query handler returning a paginated list of actors.
// ABOUTME: Maps actor entities to ActorListDto via AutoMapper.
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Actor;
using Explore.Application.Features.Actors.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Actors.Handlers.Queries;

public class GetActorListRequestHandler : IRequestHandler<GetActorListRequest, PaginatedResult<ActorListDto>>
{
    private readonly IActorRepository _actorRepository;
    private readonly IMapper _mapper;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<GetActorListRequestHandler> _logger;

    public GetActorListRequestHandler(
        IActorRepository actorRepository,
        IMapper mapper,
        IObjectStorageService objectStorageService,
        ILogger<GetActorListRequestHandler> logger)
    {
        _actorRepository = actorRepository;
        _mapper = mapper;
        _objectStorageService = objectStorageService;
        _logger = logger;
    }

    public async Task<PaginatedResult<ActorListDto>> Handle(GetActorListRequest request, CancellationToken cancellationToken)
    {
        var (pageNumber, pageSize) = PaginatedResult<ActorListDto>.NormalizeParameters(request.PageNumber, request.PageSize);
        var (actors, totalCount) = await _actorRepository.GetActorsWithDetailsPaged(pageNumber, pageSize);
        var dtos = _mapper.Map<List<ActorListDto>>(actors);

        // Resolve presigned URLs for profile pictures
        foreach (var dto in dtos)
        {
            dto.ProfilePictureUri = await ResolveImageUrl(dto.ProfilePictureUri);
        }

        return PaginatedResult<ActorListDto>.Create(dtos, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Resolves an image object key to a presigned URL for viewing.
    /// </summary>
    private async Task<string?> ResolveImageUrl(string? objectKeyOrUri)
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
                    return await _objectStorageService.GeneratePresignedDownloadUrl(objectKey, 60);
                }
                return objectKeyOrUri;
            }

            // It's an object key - generate presigned URL
            return await _objectStorageService.GeneratePresignedDownloadUrl(objectKeyOrUri, 60);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate presigned URL for object key: {ObjectKey}", objectKeyOrUri);
            return null;
        }
    }
}

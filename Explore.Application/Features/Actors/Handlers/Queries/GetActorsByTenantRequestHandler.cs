// ABOUTME: Query handler returning all actors belonging to a specific tenant.
// ABOUTME: Used for tenant-scoped actor resolution.
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Actor;
using Explore.Application.Features.Actors.Requests.Queries;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Actors.Handlers.Queries;

public class GetActorsByTenantRequestHandler : IRequestHandler<GetActorsByTenantRequest, List<ActorListDto>>
{
    private readonly IActorRepository _actorRepository;
    private readonly IMapper _mapper;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<GetActorsByTenantRequestHandler> _logger;

    public GetActorsByTenantRequestHandler(
        IActorRepository actorRepository,
        IMapper mapper,
        IObjectStorageService objectStorageService,
        ILogger<GetActorsByTenantRequestHandler> logger)
    {
        _actorRepository = actorRepository;
        _mapper = mapper;
        _objectStorageService = objectStorageService;
        _logger = logger;
    }

    public async Task<List<ActorListDto>> Handle(GetActorsByTenantRequest request, CancellationToken cancellationToken)
    {
        var actors = await _actorRepository.GetActorsByTenant(request.TenantId);
        var dtos = _mapper.Map<List<ActorListDto>>(actors);

        // Resolve presigned URLs for profile pictures
        foreach (var dto in dtos)
        {
            dto.ProfilePictureUri = await ResolveImageUrl(dto.ProfilePictureUri);
        }

        return dtos;
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
            // Check if it's a relative path (local API endpoint)
            if (objectKeyOrUri.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                return objectKeyOrUri;
            }

            // Check if it's already a full URL (legacy data or absolute local API path)
            if (objectKeyOrUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                objectKeyOrUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                if (Uri.TryCreate(objectKeyOrUri, UriKind.Absolute, out var uri))
                {
                    // If it is a local API endpoint, return it as-is
                    if (uri.AbsolutePath.StartsWith("/api/storageobject/", StringComparison.OrdinalIgnoreCase))
                    {
                        return objectKeyOrUri;
                    }

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

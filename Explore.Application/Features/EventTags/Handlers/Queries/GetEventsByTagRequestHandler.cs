// ABOUTME: Query handler returning events associated with a specific tag.
// ABOUTME: Used for tag-filtered event browsing.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.EventTags.Requests.Queries;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.EventTags.Handlers.Queries;

public class GetEventsByTagRequestHandler : IRequestHandler<GetEventsByTagRequest, List<EventListDto>>
{
    private readonly IEventTagsRepository _eventTagsRepository;
    private readonly IMapper _mapper;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<GetEventsByTagRequestHandler> _logger;

    public GetEventsByTagRequestHandler(
        IEventTagsRepository eventTagsRepository,
        IMapper mapper,
        IObjectStorageService objectStorageService,
        ILogger<GetEventsByTagRequestHandler> logger)
    {
        _eventTagsRepository = eventTagsRepository;
        _mapper = mapper;
        _objectStorageService = objectStorageService;
        _logger = logger;
    }

    public async Task<List<EventListDto>> Handle(GetEventsByTagRequest request, CancellationToken cancellationToken)
    {
        var events = await _eventTagsRepository.GetEventsByTag(request.TagId);
        var eventDtos = _mapper.Map<List<EventListDto>>(events);

        // Resolve presigned URLs for images
        foreach (var dto in eventDtos)
        {
            dto.FeaturedImageUri = await ResolveImageUrl(dto.FeaturedImageUri);
            dto.ActorProfilePictureUri = await ResolveImageUrl(dto.ActorProfilePictureUri);
        }

        return eventDtos;
    }

    /// <summary>
    /// Resolves an image object key to a presigned URL for viewing.
    /// If the value is already a full URL (legacy data), extracts the key and generates presigned URL.
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

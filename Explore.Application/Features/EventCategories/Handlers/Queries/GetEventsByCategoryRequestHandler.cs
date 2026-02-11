using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.EventCategories.Requests.Queries;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.EventCategories.Handlers.Queries;

public class GetEventsByCategoryRequestHandler : IRequestHandler<GetEventsByCategoryRequest, List<EventListDto>>
{
    private readonly IEventCategoriesRepository _eventCategoriesRepository;
    private readonly IMapper _mapper;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<GetEventsByCategoryRequestHandler> _logger;

    public GetEventsByCategoryRequestHandler(
        IEventCategoriesRepository eventCategoriesRepository,
        IMapper mapper,
        IObjectStorageService objectStorageService,
        ILogger<GetEventsByCategoryRequestHandler> logger)
    {
        _eventCategoriesRepository = eventCategoriesRepository;
        _mapper = mapper;
        _objectStorageService = objectStorageService;
        _logger = logger;
    }

    public async Task<List<EventListDto>> Handle(GetEventsByCategoryRequest request, CancellationToken cancellationToken)
    {
        var events = await _eventCategoriesRepository.GetEventsByCategory(request.CategoryId);
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
            // Check if it's already a full URL (legacy data from before this change)
            if (objectKeyOrUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                objectKeyOrUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                // Extract object key from full URL and generate presigned URL
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

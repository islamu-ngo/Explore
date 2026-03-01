using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Category;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.Tag;
using Explore.Application.Features.Events.Requests.Queries;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Events.Handlers.Queries;

public class GetEventDetailsRequestHandler : IRequestHandler<GetEventDetailsRequest, EventDto>
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventTagsRepository _eventTagsRepository;
    private readonly IEventCategoriesRepository _eventCategoriesRepository;
    private readonly IMapper _mapper;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<GetEventDetailsRequestHandler> _logger;
    private readonly HybridCache _cache;

    public GetEventDetailsRequestHandler(
        IEventRepository eventRepository,
        IEventTagsRepository eventTagsRepository,
        IEventCategoriesRepository eventCategoriesRepository,
        IMapper mapper,
        IObjectStorageService objectStorageService,
        ILogger<GetEventDetailsRequestHandler> logger,
        HybridCache cache)
    {
        _eventRepository = eventRepository;
        _eventTagsRepository = eventTagsRepository;
        _eventCategoriesRepository = eventCategoriesRepository;
        _mapper = mapper;
        _objectStorageService = objectStorageService;
        _logger = logger;
        _cache = cache;
    }

    public async Task<EventDto> Handle(GetEventDetailsRequest request, CancellationToken cancellationToken)
    {
        var cacheKey = $"event:detail:{request.Id}";

        var eventDto = await _cache.GetOrCreateAsync(
            cacheKey,
            async _ =>
            {
                var @event = await _eventRepository.GetEventWithDetails(request.Id);
                var dto = _mapper.Map<EventDto>(@event);

                if (dto != null)
                {
                    var tags = await _eventTagsRepository.GetTagsByEvent(request.Id);
                    var categories = await _eventCategoriesRepository.GetCategoriesByEvent(request.Id);

                    dto.Tags = _mapper.Map<List<TagListDto>>(tags);
                    dto.Categories = _mapper.Map<List<CategoryListDto>>(categories);
                }

                return dto;
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            },
            cancellationToken: cancellationToken);

        // Resolve presigned URLs for images
        if (eventDto != null)
        {
            eventDto.FeaturedImageUri = await ResolveImageUrl(eventDto.FeaturedImageUri);
            eventDto.ActorProfilePictureUri = await ResolveImageUrl(eventDto.ActorProfilePictureUri);
        }

        return eventDto;
    }

    /// <summary>
    /// Resolves an image object key to a presigned URL for viewing.
    /// If the value is already a full URL (legacy data), returns it as-is.
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

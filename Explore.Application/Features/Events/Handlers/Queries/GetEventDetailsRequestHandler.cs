// ABOUTME: Query handler returning full event details by ID or slug.
// ABOUTME: Maps Event entity to EventDto with nested sessions and speakers.
using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Category;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.Tag;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Domain.Enums;
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
    private readonly IUserContext _userContext;

    public GetEventDetailsRequestHandler(
        IEventRepository eventRepository,
        IEventTagsRepository eventTagsRepository,
        IEventCategoriesRepository eventCategoriesRepository,
        IMapper mapper,
        IObjectStorageService objectStorageService,
        ILogger<GetEventDetailsRequestHandler> logger,
        HybridCache cache,
        IUserContext userContext)
    {
        _eventRepository = eventRepository;
        _eventTagsRepository = eventTagsRepository;
        _eventCategoriesRepository = eventCategoriesRepository;
        _mapper = mapper;
        _objectStorageService = objectStorageService;
        _logger = logger;
        _cache = cache;
        _userContext = userContext;
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

        if (eventDto is null)
            return eventDto;

        // Visibility enforcement: Archived events are not publicly accessible
        if (eventDto.EventStatusId == (int)EventStatusEnum.Archived)
            return null;

        // Visibility enforcement: Draft events are only visible to their creator
        if (eventDto.EventStatusId == (int)EventStatusEnum.Draft)
        {
            var currentUserId = _userContext.UserId;
            if (currentUserId is null)
                return null;

            var @event = await _eventRepository.GetEventWithDetails(request.Id);
            if (@event?.CreatedBy != currentUserId)
                return null;
        }

        // Resolve presigned URLs for images
        eventDto.FeaturedImageUri = await ResolveImageUrl(eventDto.FeaturedImageUri);
        eventDto.ActorProfilePictureUri = await ResolveImageUrl(eventDto.ActorProfilePictureUri);

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

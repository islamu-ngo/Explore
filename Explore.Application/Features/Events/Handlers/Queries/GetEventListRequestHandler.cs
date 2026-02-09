using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Events.Handlers.Queries;

public class GetEventListRequestHandler : IRequestHandler<GetEventListRequest, PaginatedResult<EventListDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<GetEventListRequestHandler> _logger;
    private readonly HybridCache _cache;

    public GetEventListRequestHandler(
        IEventRepository eventRepository,
        IMapper mapper,
        IObjectStorageService objectStorageService,
        ILogger<GetEventListRequestHandler> logger,
        HybridCache cache)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
        _objectStorageService = objectStorageService;
        _logger = logger;
        _cache = cache;
    }

    public async Task<PaginatedResult<EventListDto>> Handle(GetEventListRequest request, CancellationToken cancellationToken)
    {
        var cacheKey = $"events:list:{request.PageNumber}:{request.PageSize}";
        var cachedResult = await _cache.GetOrCreateAsync(
            cacheKey,
            async _ =>
            {
                var (events, totalCount) = await _eventRepository.GetEventsWithDetailsPaged(request.PageNumber, request.PageSize);
                var eventDtos = _mapper.Map<List<EventListDto>>(events);
                return PaginatedResult<EventListDto>.Create(eventDtos, totalCount, request.PageNumber, request.PageSize);
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            },
            cancellationToken: cancellationToken);

        // Resolve presigned URLs for images
        foreach (var dto in cachedResult.Items)
        {
            dto.FeaturedImageUri = ResolveImageUrl(dto.FeaturedImageUri);
            dto.ActorProfilePictureUri = ResolveImageUrl(dto.ActorProfilePictureUri);
        }

        return cachedResult;
    }

    /// <summary>
    /// Resolves an image object key to a presigned URL for viewing.
    /// If the value is already a full URL (legacy data), extracts the key and generates presigned URL.
    /// </summary>
    private string? ResolveImageUrl(string? objectKeyOrUri)
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

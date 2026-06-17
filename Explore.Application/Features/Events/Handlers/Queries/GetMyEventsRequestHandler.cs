// ABOUTME: Query handler returning events created or managed by the current user.
// ABOUTME: Filters by actor ID and maps to EventListDto.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Events.Handlers.Queries;

public class GetMyEventsRequestHandler : IRequestHandler<GetMyEventsRequest, PaginatedResult<EventListDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<GetMyEventsRequestHandler> _logger;

    public GetMyEventsRequestHandler(
        IEventRepository eventRepository,
        IMapper mapper,
        IObjectStorageService objectStorageService,
        ILogger<GetMyEventsRequestHandler> logger)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
        _objectStorageService = objectStorageService;
        _logger = logger;
    }

    public async Task<PaginatedResult<EventListDto>> Handle(GetMyEventsRequest request, CancellationToken cancellationToken)
    {
        var (events, totalCount) = await _eventRepository.GetMyEventsWithDetailsPaged(request.UserId, request.PageNumber, request.PageSize);
        var eventDtos = _mapper.Map<List<EventListDto>>(events);

        // Resolve presigned URLs for images
        foreach (var dto in eventDtos)
        {
            dto.FeaturedImageUri = await ResolveImageUrl(dto.FeaturedImageUri);
            dto.ActorProfilePictureUri = await ResolveImageUrl(dto.ActorProfilePictureUri);
        }

        return PaginatedResult<EventListDto>.Create(eventDtos, totalCount, request.PageNumber, request.PageSize);
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

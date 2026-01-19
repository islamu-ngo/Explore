using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Queries;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Events.Handlers.Queries
{
    public class GetEventDetailsRequestHandler : IRequestHandler<GetEventDetailsRequest, EventDto>
    {
        private readonly IEventRepository _eventRepository;
        private readonly IMapper _mapper;
        private readonly IObjectStorageService _objectStorageService;
        private readonly ILogger<GetEventDetailsRequestHandler> _logger;

        public GetEventDetailsRequestHandler(
            IEventRepository eventRepository,
            IMapper mapper,
            IObjectStorageService objectStorageService,
            ILogger<GetEventDetailsRequestHandler> logger)
        {
            _eventRepository = eventRepository;
            _mapper = mapper;
            _objectStorageService = objectStorageService;
            _logger = logger;
        }

        public async Task<EventDto> Handle(GetEventDetailsRequest request, CancellationToken cancellationToken)
        {
            var @event = await _eventRepository.GetEventWithDetails(request.Id);
            var eventDto = _mapper.Map<EventDto>(@event);

            // Resolve presigned URLs for images
            if (eventDto != null)
            {
                eventDto.FeaturedImageUri = ResolveImageUrl(eventDto.FeaturedImageUri);
                eventDto.ActorProfilePictureUri = ResolveImageUrl(eventDto.ActorProfilePictureUri);
            }

            return eventDto;
        }

        /// <summary>
        /// Resolves an image object key to a presigned URL for viewing.
        /// If the value is already a full URL (legacy data), returns it as-is.
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
}

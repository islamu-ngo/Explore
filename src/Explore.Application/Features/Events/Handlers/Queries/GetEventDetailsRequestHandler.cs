// ABOUTME: Query handler returning full event details by ID or slug.
// ABOUTME: Maps Event entity to EventDto with nested sessions and speakers.
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.Events.Handlers.Queries;

public class GetEventDetailsRequestHandler : IRequestHandler<GetEventDetailsRequest, EventDto>
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventDetailsProjectionService _detailsProjectionService;
    private readonly HybridCache _cache;
    private readonly IUserContext _userContext;

    public GetEventDetailsRequestHandler(
        IEventRepository eventRepository,
        IEventDetailsProjectionService detailsProjectionService,
        HybridCache cache,
        IUserContext userContext)
    {
        _eventRepository = eventRepository;
        _detailsProjectionService = detailsProjectionService;
        _cache = cache;
        _userContext = userContext;
    }

    public async Task<EventDto> Handle(GetEventDetailsRequest request, CancellationToken cancellationToken)
    {
        var cacheKey = $"event:detail:{request.Id}";

        var eventDto = await _cache.GetOrCreateAsync(
            cacheKey,
            async token =>
            {
                return await _detailsProjectionService.BuildAsync(request.Id, token);
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            },
            cancellationToken: cancellationToken);

        if (eventDto is null)
            return eventDto;

        if (eventDto.EventStatusId is (int)EventStatusEnum.Archived or (int)EventStatusEnum.Moderated)
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

        await _detailsProjectionService.ResolveImageUrlsAsync(eventDto, cancellationToken);

        return eventDto;
    }
}

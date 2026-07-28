// ABOUTME: Query handler returning full event details by ID or slug.
// ABOUTME: Maps Event entity to EventDto with nested sessions and speakers.
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Queries;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.Events.Handlers.Queries;

public class GetEventDetailsRequestHandler : IRequestHandler<GetEventDetailsRequest, EventDto>
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventDetailsProjectionService _detailsProjectionService;
    private readonly HybridCache _cache;

    public GetEventDetailsRequestHandler(
        IEventRepository eventRepository,
        IEventDetailsProjectionService detailsProjectionService,
        HybridCache cache)
    {
        _eventRepository = eventRepository;
        _detailsProjectionService = detailsProjectionService;
        _cache = cache;
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

        var isPubliclyEligible = await _eventRepository.IsPubliclyEligibleAsync(
            eventDto.TenantId,
            eventDto.Id,
            cancellationToken);

        if (!isPubliclyEligible)
            return null;

        var responseDto = eventDto.CreateRequestCopy();
        responseDto.IsPubliclyEligible = true;
        responseDto.IsManagementView = false;
        await _detailsProjectionService.ResolveImageUrlsAsync(responseDto, cancellationToken);

        return responseDto;
    }
}

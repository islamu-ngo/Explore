// ABOUTME: Query handler for public event detail lookup by slug-code URL token.
// ABOUTME: Resolves by server-owned public code while keeping public visibility checks strict.

using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.Events.Handlers.Queries;

public sealed class GetPublicEventDetailsRequestHandler(
    IEventDetailsProjectionService detailsProjectionService,
    HybridCache cache)
    : IRequestHandler<GetPublicEventDetailsRequest, EventDto?>
{
    public async Task<EventDto?> Handle(GetPublicEventDetailsRequest request, CancellationToken cancellationToken)
    {
        var publicCode = ExtractPublicCode(request.SlugCode);
        if (publicCode is null)
            return null;

        var eventDto = await cache.GetOrCreateAsync(
            $"event:public-detail:{publicCode}",
            async token => await detailsProjectionService.BuildByPublicCodeAsync(publicCode, token),
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            },
            cancellationToken: cancellationToken);

        if (eventDto is null)
            return null;

        if (eventDto.EventStatusId is not (int)EventStatusEnum.Published)
            return null;

        await detailsProjectionService.ResolveImageUrlsAsync(eventDto, cancellationToken);
        return eventDto;
    }

    private static string? ExtractPublicCode(string slugCode)
    {
        if (string.IsNullOrWhiteSpace(slugCode))
            return null;

        var separatorIndex = slugCode.LastIndexOf('-');
        if (separatorIndex < 0 || separatorIndex == slugCode.Length - 1)
            return null;

        return slugCode[(separatorIndex + 1)..];
    }
}

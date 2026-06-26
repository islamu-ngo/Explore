// ABOUTME: Handles authorized management event detail reads, including moderated events.
// ABOUTME: Reuses the shared projection service so management and public detail DTOs stay aligned.

using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.Events.Handlers.Queries;

public sealed class GetEventManagementDetailsRequestHandler : IRequestHandler<GetEventManagementDetailsRequest, EventDto?>
{
    private readonly IEventDetailsProjectionService _detailsProjectionService;

    public GetEventManagementDetailsRequestHandler(IEventDetailsProjectionService detailsProjectionService)
    {
        _detailsProjectionService = detailsProjectionService;
    }

    public async Task<EventDto?> Handle(GetEventManagementDetailsRequest request, CancellationToken cancellationToken)
    {
        var eventDto = await _detailsProjectionService.BuildAsync(request.Id, cancellationToken);
        if (eventDto is null)
            return null;

        await _detailsProjectionService.ResolveImageUrlsAsync(eventDto, cancellationToken);
        return eventDto;
    }
}

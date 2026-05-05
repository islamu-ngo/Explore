using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Application.Services;
using MediatR;

namespace Explore.Application.Features.Events.Handlers.Queries;

public class GetEventPublishReadinessRequestHandler(IEventRepository eventRepository)
    : IRequestHandler<GetEventPublishReadinessRequest, EventPublishReadinessDto?>
{
    public async Task<EventPublishReadinessDto?> Handle(GetEventPublishReadinessRequest request, CancellationToken cancellationToken)
    {
        var @event = await eventRepository.GetById(request.Id);
        return @event is null ? null : EventPublishReadinessEvaluator.Evaluate(@event);
    }
}

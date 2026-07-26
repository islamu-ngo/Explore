// ABOUTME: Resolves one public action only when its parent event and review state are public.
// ABOUTME: Fails closed for missing, pending, unsafe, disabled, private, or unpublished destinations.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.EventPublicActions.Requests.Queries;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.EventPublicActions.Handlers.Queries;

public sealed class GetEventPublicActionRequestHandler(
    IEventRepository eventRepository,
    IEventPublicActionRepository actionRepository,
    IMapper mapper)
    : IRequestHandler<GetEventPublicActionRequest, EventPublicActionDto?>
{
    public async Task<EventPublicActionDto?> Handle(
        GetEventPublicActionRequest request,
        CancellationToken cancellationToken)
    {
        var @event = await eventRepository.GetById(request.EventId);
        if (@event is null
            || @event.EventStatusId != (int)EventStatusEnum.Published
            || @event.VisibilityTypeId != (int)VisibilityTypeEnum.Public)
        {
            return null;
        }

        var action = await actionRepository.GetDetailsAsync(
            request.ActionId,
            trackChanges: false,
            cancellationToken);
        return action is not null
            && action.EventId == request.EventId
            && action.HealthStateId == (int)EventPublicActionHealthStateEnum.Active
                ? mapper.Map<EventPublicActionDto>(action)
                : null;
    }
}

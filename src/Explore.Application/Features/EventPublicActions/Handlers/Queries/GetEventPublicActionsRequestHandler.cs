// ABOUTME: Returns ordered event public actions with normalized lookup metadata.
// ABOUTME: Repository query filters keep public reads tenant-scoped and soft-delete-aware.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.EventPublicActions.Requests.Queries;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.EventPublicActions.Handlers.Queries;

public sealed class GetEventPublicActionsRequestHandler(
    IEventRepository eventRepository,
    IEventPublicActionRepository actionRepository,
    IMapper mapper)
    : IRequestHandler<GetEventPublicActionsRequest, IReadOnlyList<EventPublicActionDto>>
{
    public async Task<IReadOnlyList<EventPublicActionDto>> Handle(
        GetEventPublicActionsRequest request,
        CancellationToken cancellationToken)
    {
        var @event = await eventRepository.GetById(request.EventId);
        if (@event is null
            || @event.EventStatusId != (int)EventStatusEnum.Published
            || @event.VisibilityTypeId != (int)VisibilityTypeEnum.Public)
        {
            return [];
        }

        var actions = await actionRepository.ListByEventAsync(
            request.EventId,
            trackChanges: false,
            cancellationToken);
        return mapper.Map<List<EventPublicActionDto>>(
            actions.Where(action => action.HealthStateId == (int)EventPublicActionHealthStateEnum.Active));
    }
}

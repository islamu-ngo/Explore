// ABOUTME: Handler for grouped EventAgendaItem PATCH updates with validation and local projection.
// ABOUTME: Applies explicit groups, checks concurrency, re-links EventDayId, and invalidates parent event caches.

using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventAgendaItem;
using Explore.Application.DTOs.EventAgendaItem.Validators;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventAgendaItems.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Services.Scheduling;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.EventAgendaItems.Handlers.Commands;

public class UpdateEventAgendaItemCommandHandler : IRequestHandler<UpdateEventAgendaItemCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventAgendaItemRepository _eventAgendaItemRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IEventDayRepository _eventDayRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly ILocationRoomRepository _locationRoomRepository;
    private readonly IScheduleItemKindRepository _scheduleItemKindRepository;
    private readonly IEventScheduleProjectionCalculator _scheduleProjectionCalculator;
    private readonly HybridCache _cache;

    public UpdateEventAgendaItemCommandHandler(
        IEventAgendaItemRepository eventAgendaItemRepository,
        IEventRepository eventRepository,
        IEventDayRepository eventDayRepository,
        ILocationRepository locationRepository,
        ILocationRoomRepository locationRoomRepository,
        IScheduleItemKindRepository scheduleItemKindRepository,
        IEventScheduleProjectionCalculator scheduleProjectionCalculator,
        HybridCache cache)
    {
        _eventAgendaItemRepository = eventAgendaItemRepository;
        _eventRepository = eventRepository;
        _eventDayRepository = eventDayRepository;
        _locationRepository = locationRepository;
        _locationRoomRepository = locationRoomRepository;
        _scheduleItemKindRepository = scheduleItemKindRepository;
        _scheduleProjectionCalculator = scheduleProjectionCalculator;
        _cache = cache;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventAgendaItemCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateEventAgendaItemDtoValidator(
            _eventRepository,
            _locationRepository,
            _locationRoomRepository,
            _scheduleItemKindRepository);
        var validationResult = await validator.ValidateAsync(request.EventAgendaItemDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event agenda item update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var agendaItem = await _eventAgendaItemRepository.GetById(request.EventAgendaItemId);
        if (agendaItem == null)
        {
            response.Success = false;
            response.Message = "Event agenda item not found.";
            return response;
        }

        if (agendaItem.ConcurrencyStamp != request.ExpectedConcurrencyStamp)
        {
            throw new ConcurrencyConflictException(
                ConcurrencyConflictException.ConcurrentUpdate,
                "The event agenda item was modified by another request. Reload and retry.",
                nameof(EventAgendaItem),
                agendaItem.Id.ToString());
        }

        var previousEventId = agendaItem.EventId;
        var parentEventId = request.EventAgendaItemDto.Event?.EventId ?? agendaItem.EventId;
        var parentEvent = await _eventRepository.GetById(parentEventId);
        if (parentEvent == null || parentEvent.TenantId != agendaItem.TenantId)
        {
            response.Success = false;
            response.Message = "Event does not belong to the same tenant as the agenda item.";
            return response;
        }

        var relationshipValidation = await ValidateLocationRoomRelationshipAsync(agendaItem, request.EventAgendaItemDto, cancellationToken);
        if (!relationshipValidation.Success)
        {
            response.Success = false;
            response.Message = "Event agenda item update failed.";
            response.Errors = [relationshipValidation.Message];
            return response;
        }

        var eventChanged = previousEventId != parentEvent.Id;
        ApplyEvent(agendaItem, request.EventAgendaItemDto.Event);
        ApplyTitle(agendaItem, request.EventAgendaItemDto.Title);
        ApplyDescription(agendaItem, request.EventAgendaItemDto.Description);
        ApplyLocationRoom(agendaItem, request.EventAgendaItemDto, relationshipValidation.LocationId, relationshipValidation.RoomId);
        ApplyKind(agendaItem, request.EventAgendaItemDto.Kind);
        ApplySortOrder(agendaItem, request.EventAgendaItemDto.SortOrder);
        await ApplyScheduleAsync(agendaItem, parentEvent, eventChanged, request.EventAgendaItemDto.Schedule, cancellationToken);

        await _eventAgendaItemRepository.Update(agendaItem);

        if (eventChanged)
        {
            await _cache.RemoveAsync($"event:detail:{previousEventId}", cancellationToken);
        }

        await _cache.RemoveAsync($"event:detail:{parentEvent.Id}", cancellationToken);
        await _cache.RemoveByTagAsync(CacheTags.EventListByTenant(parentEvent.TenantId), cancellationToken);

        response.Success = true;
        response.Id = agendaItem.Id;
        response.Message = "Event agenda item updated successfully.";

        return response;
    }

    private async Task<(bool Success, string Message, Guid? LocationId, Guid? RoomId)> ValidateLocationRoomRelationshipAsync(
        EventAgendaItem agendaItem,
        UpdateEventAgendaItemDto dto,
        CancellationToken cancellationToken)
    {
        var finalLocationId = dto.Location?.Value.HasValue == true ? dto.Location.Value.Value : agendaItem.LocationId;
        var finalRoomId = dto.Room?.Value.HasValue == true ? dto.Room.Value.Value : agendaItem.RoomId;

        if (finalLocationId.HasValue)
        {
            var location = await _locationRepository.GetById(finalLocationId.Value);
            if (location == null || location.TenantId != agendaItem.TenantId)
            {
                return (false, "Location does not belong to the same tenant as the agenda item.", finalLocationId, finalRoomId);
            }
        }

        if (finalRoomId.HasValue)
        {
            var room = await _locationRoomRepository.GetById(finalRoomId.Value);
            if (room == null || room.TenantId != agendaItem.TenantId)
            {
                return (false, "Room does not belong to the same tenant as the agenda item.", finalLocationId, finalRoomId);
            }

            if (!finalLocationId.HasValue)
            {
                finalLocationId = room.LocationId;
            }
            else if (finalLocationId.Value != room.LocationId)
            {
                return (false, "Room must belong to the selected location.", finalLocationId, finalRoomId);
            }
        }

        return (true, string.Empty, finalLocationId, finalRoomId);
    }

    private async Task ApplyScheduleAsync(
        EventAgendaItem agendaItem,
        Event parentEvent,
        bool eventChanged,
        UpdateEventAgendaItemScheduleDto? group,
        CancellationToken cancellationToken)
    {
        if (group is not null)
        {
            agendaItem.Reschedule(
                group.StartTime,
                group.EndTime,
                parentEvent.EventTimeZoneId ?? parentEvent.Timezone ?? string.Empty,
                _scheduleProjectionCalculator);
            await RelinkEventDayAsync(agendaItem, parentEvent.Id, cancellationToken);
            return;
        }

        if (!eventChanged)
        {
            return;
        }

        agendaItem.ReprojectLocalTimes(parentEvent.EventTimeZoneId ?? parentEvent.Timezone ?? string.Empty, _scheduleProjectionCalculator);
        await RelinkEventDayAsync(agendaItem, parentEvent.Id, cancellationToken);
    }

    private async Task RelinkEventDayAsync(EventAgendaItem agendaItem, Guid eventId, CancellationToken cancellationToken)
    {
        var matchingDay = await _eventDayRepository.FindByEventAndLocalDateAsync(
            eventId,
            agendaItem.LocalStartDate,
            cancellationToken);
        agendaItem.EventDayId = matchingDay?.Id;
    }

    private static void ApplyEvent(EventAgendaItem agendaItem, UpdateEventAgendaItemEventDto? group)
    {
        if (group is not null)
        {
            agendaItem.EventId = group.EventId;
        }
    }

    private static void ApplyTitle(EventAgendaItem agendaItem, UpdateEventAgendaItemTitleDto? group)
    {
        if (group is not null)
        {
            agendaItem.Title = group.Value;
        }
    }

    private static void ApplyDescription(EventAgendaItem agendaItem, UpdateEventAgendaItemDescriptionDto? group)
    {
        if (group?.Value.HasValue == true)
        {
            agendaItem.Description = group.Value.Value;
        }
    }

    private static void ApplyLocationRoom(
        EventAgendaItem agendaItem,
        UpdateEventAgendaItemDto dto,
        Guid? locationId,
        Guid? roomId)
    {
        if (dto.Location is not null || dto.Room is not null)
        {
            agendaItem.LocationId = locationId;
            agendaItem.RoomId = roomId;
        }
    }

    private static void ApplyKind(EventAgendaItem agendaItem, UpdateEventAgendaItemKindDto? group)
    {
        if (group?.Value.HasValue == true)
        {
            agendaItem.KindId = group.Value.Value;
        }
    }

    private static void ApplySortOrder(EventAgendaItem agendaItem, UpdateEventAgendaItemSortOrderDto? group)
    {
        if (group is not null)
        {
            agendaItem.SortOrder = group.Value;
        }
    }
}

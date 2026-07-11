// ABOUTME: Handler for grouped EventSession PATCH updates.
// ABOUTME: Applies explicit groups, preserves schedule projections, and saves session/aspect atomically.

using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSession;
using Explore.Application.DTOs.EventSession.Validators;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventSessions.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Services.Scheduling;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.EventSessions.Handlers.Commands;

public class UpdateEventSessionCommandHandler : IRequestHandler<UpdateEventSessionCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly ILocationRoomRepository _locationRoomRepository;
    private readonly IRegistrationModeRepository _registrationModeRepository;
    private readonly IEventSessionKindRepository _eventSessionKindRepository;
    private readonly IEventSessionIslamicAspectRepository _eventSessionIslamicAspectRepository;
    private readonly IEventScheduleProjectionCalculator _scheduleProjectionCalculator;
    private readonly IEventDayRepository _eventDayRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly HybridCache _cache;

    public UpdateEventSessionCommandHandler(
        IEventSessionRepository eventSessionRepository,
        IEventRepository eventRepository,
        ILocationRepository locationRepository,
        ILocationRoomRepository locationRoomRepository,
        IRegistrationModeRepository registrationModeRepository,
        IEventSessionKindRepository eventSessionKindRepository,
        IEventSessionIslamicAspectRepository eventSessionIslamicAspectRepository,
        IEventScheduleProjectionCalculator scheduleProjectionCalculator,
        IEventDayRepository eventDayRepository,
        IUnitOfWork unitOfWork,
        HybridCache cache)
    {
        _eventSessionRepository = eventSessionRepository;
        _eventRepository = eventRepository;
        _locationRepository = locationRepository;
        _locationRoomRepository = locationRoomRepository;
        _registrationModeRepository = registrationModeRepository;
        _eventSessionKindRepository = eventSessionKindRepository;
        _eventSessionIslamicAspectRepository = eventSessionIslamicAspectRepository;
        _scheduleProjectionCalculator = scheduleProjectionCalculator;
        _eventDayRepository = eventDayRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventSessionCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateEventSessionDtoValidator(
            _eventRepository,
            _locationRepository,
            _locationRoomRepository,
            _registrationModeRepository,
            _eventSessionKindRepository);
        var validationResult = await validator.ValidateAsync(request.EventSessionDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event session update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var eventSession = await _eventSessionRepository.GetById(request.EventSessionId);
        if (eventSession == null)
        {
            response.Success = false;
            response.Message = "Event session not found.";
            return response;
        }

        if (eventSession.ConcurrencyStamp != request.ExpectedConcurrencyStamp)
        {
            throw new ConcurrencyConflictException(
                ConcurrencyConflictException.ConcurrentUpdate,
                "The event session was modified by another request. Reload and retry.",
                nameof(EventSession),
                eventSession.Id.ToString());
        }

        var previousEventId = eventSession.EventId;
        var parentEventId = request.EventSessionDto.Event?.EventId ?? eventSession.EventId;
        var parentEvent = await _eventRepository.GetById(parentEventId);
        if (parentEvent == null || parentEvent.TenantId != eventSession.TenantId)
        {
            response.Success = false;
            response.Message = "Event does not belong to the same tenant as the session.";
            return response;
        }

        if (!HasValidFinalIslamicSchedulingState(eventSession, request.EventSessionDto))
        {
            response.Success = false;
            response.Message = "Event session update failed.";
            response.Errors = [EventSessionIslamicAspectValidationRules.SchedulingStateMessage];
            return response;
        }

        var eventChanged = previousEventId != parentEvent.Id;
        ApplyEvent(eventSession, request.EventSessionDto.Event);
        ApplyLocation(eventSession, request.EventSessionDto.Location);
        ApplyFeaturedImage(eventSession, request.EventSessionDto.FeaturedImage);
        ApplyRoom(eventSession, request.EventSessionDto.Room);
        ApplySortOrder(eventSession, request.EventSessionDto.SortOrder);
        ApplyTitle(eventSession, request.EventSessionDto.Title);
        ApplyKind(eventSession, request.EventSessionDto.Kind);
        ApplyDescription(eventSession, request.EventSessionDto.Description);
        ApplySlug(eventSession, request.EventSessionDto.Slug);
        ApplyMaxAudienceAttendees(eventSession, request.EventSessionDto.MaxAudienceAttendees);
        ApplyRegistrationMode(eventSession, request.EventSessionDto.RegistrationMode);
        ApplyPrice(eventSession, request.EventSessionDto.Price);
        ApplyCurrencyCode(eventSession, request.EventSessionDto.CurrencyCode);
        await ApplyScheduleAsync(eventSession, parentEvent, eventChanged, request.EventSessionDto.Schedule, cancellationToken);

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                await _eventSessionRepository.UpdateWithRoomOverlapGuardAsync(eventSession, token);
                await ApplyIslamicAspectAsync(eventSession.Id, request.EventSessionDto.IslamicAspect, eventSession.EndTimeType, token);
            }, cancellationToken);
        }
        catch (RoomScheduleConflictException ex)
        {
            response.Success = false;
            response.Message = "Event session update failed.";
            response.Errors = [ex.Message];
            response.FailureCode = "room_schedule_conflict";
            return response;
        }

        if (eventChanged)
        {
            await _cache.RemoveAsync($"event:detail:{previousEventId}", cancellationToken);
        }

        await _cache.RemoveAsync($"event:detail:{parentEvent.Id}", cancellationToken);
        await _cache.RemoveByTagAsync(CacheTags.EventListByTenant(parentEvent.TenantId), cancellationToken);

        response.Success = true;
        response.Id = eventSession.Id;
        response.Message = "Event session updated successfully.";

        return response;
    }

    private async Task ApplyScheduleAsync(
        EventSession eventSession,
        Event parentEvent,
        bool eventChanged,
        UpdateEventSessionScheduleDto? group,
        CancellationToken cancellationToken)
    {
        if (group is null)
        {
            if (!eventChanged)
            {
                return;
            }

            eventSession.ReprojectLocalTimes(parentEvent.EventTimeZoneId ?? parentEvent.Timezone ?? string.Empty, _scheduleProjectionCalculator);
            await RelinkEventDayAsync(eventSession, parentEvent.Id, cancellationToken);
            return;
        }

        if (group.EndTimeType.HasValue)
        {
            eventSession.EndTimeType = group.EndTimeType.Value;
        }

        if (group.StartTime.Value is null || group.EndTime.Value is null)
        {
            eventSession.StartTime = null;
            eventSession.EndTime = null;
            eventSession.ReprojectLocalTimes(parentEvent.EventTimeZoneId ?? parentEvent.Timezone ?? string.Empty, _scheduleProjectionCalculator);
            eventSession.EventDayId = null;
            return;
        }

        eventSession.Reschedule(
            group.StartTime.Value.Value,
            group.EndTime.Value.Value,
            parentEvent.EventTimeZoneId ?? parentEvent.Timezone ?? string.Empty,
            _scheduleProjectionCalculator);
        await RelinkEventDayAsync(eventSession, parentEvent.Id, cancellationToken);
    }

    private async Task RelinkEventDayAsync(EventSession eventSession, Guid eventId, CancellationToken cancellationToken)
    {
        var matchingDay = eventSession.LocalStartDate is not null
            ? await _eventDayRepository.FindByEventAndLocalDateAsync(
                eventId,
                eventSession.LocalStartDate.Value,
                cancellationToken)
            : null;
        eventSession.EventDayId = matchingDay?.Id;
    }

    private async Task ApplyIslamicAspectAsync(
        Guid eventSessionId,
        UpdateEventSessionIslamicAspectUpdateDto? group,
        SessionEndTimeType endTimeType,
        CancellationToken cancellationToken)
    {
        if (group is null)
        {
            return;
        }

        var existingIslamicAspect = await _eventSessionIslamicAspectRepository.GetById(eventSessionId);
        if (group.Value.Value is null)
        {
            if (existingIslamicAspect != null)
            {
                await _eventSessionIslamicAspectRepository.Delete(existingIslamicAspect);
            }

            return;
        }

        if (existingIslamicAspect == null)
        {
            var newAspect = new EventSessionIslamicAspect
            {
                EventSessionId = eventSessionId,
                EventSession = null
            };
            ApplyIslamicAspectDto(newAspect, group.Value.Value, endTimeType);
            await _eventSessionIslamicAspectRepository.Create(newAspect);
            return;
        }

        ApplyIslamicAspectDto(existingIslamicAspect, group.Value.Value, endTimeType);
        await _eventSessionIslamicAspectRepository.Update(existingIslamicAspect);
    }

    private static bool HasValidFinalIslamicSchedulingState(EventSession eventSession, UpdateEventSessionDto dto)
    {
        if (dto.IslamicAspect?.Value.HasValue != true || dto.IslamicAspect.Value.Value is null)
        {
            return true;
        }

        var locationId = dto.Location?.Value.HasValue == true ? dto.Location.Value.Value : eventSession.LocationId;
        return EventSessionIslamicAspectValidationRules.HasValidSchedulingState(dto.IslamicAspect.Value.Value, locationId);
    }

    private static void ApplyEvent(EventSession eventSession, UpdateEventSessionEventDto? group)
    {
        if (group is not null)
        {
            eventSession.EventId = group.EventId;
        }
    }

    private static void ApplyLocation(EventSession eventSession, UpdateEventSessionLocationDto? group)
    {
        if (group?.Value.HasValue == true)
        {
            eventSession.LocationId = group.Value.Value;
        }
    }

    private static void ApplyFeaturedImage(EventSession eventSession, UpdateEventSessionFeaturedImageDto? group)
    {
        if (group?.Value.HasValue == true)
        {
            eventSession.FeaturedImageId = group.Value.Value;
        }
    }

    private static void ApplyRoom(EventSession eventSession, UpdateEventSessionRoomDto? group)
    {
        if (group?.Value.HasValue == true)
        {
            eventSession.RoomId = group.Value.Value;
        }
    }

    private static void ApplySortOrder(EventSession eventSession, UpdateEventSessionSortOrderDto? group)
    {
        if (group is not null)
        {
            eventSession.SortOrder = group.Value;
        }
    }

    private static void ApplyTitle(EventSession eventSession, UpdateEventSessionTitleDto? group)
    {
        if (group?.Value.HasValue == true)
        {
            eventSession.Title = group.Value.Value;
        }
    }

    private static void ApplyKind(EventSession eventSession, UpdateEventSessionKindDto? group)
    {
        if (group?.Value.HasValue == true)
        {
            eventSession.EventSessionKindId = group.Value.Value;
        }
    }

    private static void ApplyDescription(EventSession eventSession, UpdateEventSessionDescriptionDto? group)
    {
        if (group?.Value.HasValue == true)
        {
            eventSession.Description = group.Value.Value;
        }
    }

    private static void ApplySlug(EventSession eventSession, UpdateEventSessionSlugDto? group)
    {
        if (group?.Value.HasValue == true)
        {
            eventSession.Slug = group.Value.Value;
        }
    }

    private static void ApplyMaxAudienceAttendees(EventSession eventSession, UpdateEventSessionMaxAudienceAttendeesDto? group)
    {
        if (group?.Value.HasValue == true)
        {
            eventSession.MaxAudienceAttendees = group.Value.Value;
        }
    }

    private static void ApplyRegistrationMode(EventSession eventSession, UpdateEventSessionRegistrationModeDto? group)
    {
        if (group?.Value.HasValue == true)
        {
            eventSession.RegistrationModeId = group.Value.Value;
        }
    }

    private static void ApplyPrice(EventSession eventSession, UpdateEventSessionPriceDto? group)
    {
        if (group?.Value.HasValue == true)
        {
            eventSession.Price = group.Value.Value;
        }
    }

    private static void ApplyCurrencyCode(EventSession eventSession, UpdateEventSessionCurrencyCodeDto? group)
    {
        if (group?.Value.HasValue == true)
        {
            eventSession.CurrencyCode = group.Value.Value;
        }
    }

    private static void ApplyIslamicAspectDto(
        EventSessionIslamicAspect aspect,
        EventSessionIslamicAspectDto dto,
        SessionEndTimeType endTimeType)
    {
        aspect.ApplyScheduling(dto.StartTimeType, dto.ReferencePrayer, dto.OffsetMinutes);
        aspect.ApplyEndTimeScheduling(endTimeType, dto.EndReferencePrayer, dto.EndOffsetMinutes);
        aspect.RequiresWudu = dto.RequiresWudu;
        aspect.RitualRequirementsJson = dto.RitualRequirementsJson;
    }
}

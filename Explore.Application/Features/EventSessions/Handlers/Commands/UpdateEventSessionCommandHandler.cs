// ABOUTME: Handler for updating an existing event session with validation.
// ABOUTME: Validates input, fetches entity, applies field updates.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSession.Validators;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventSessions.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Services.Scheduling;
using MediatR;

namespace Explore.Application.Features.EventSessions.Handlers.Commands;

public class UpdateEventSessionCommandHandler : IRequestHandler<UpdateEventSessionCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly IRegistrationModeRepository _registrationModeRepository;
    private readonly IEventSessionKindRepository _eventSessionKindRepository;
    private readonly IEventSessionIslamicAspectRepository _eventSessionIslamicAspectRepository;
    private readonly IEventScheduleProjectionCalculator _scheduleProjectionCalculator;
    private readonly IEventDayRepository _eventDayRepository;
    private readonly IMapper _mapper;

    public UpdateEventSessionCommandHandler(
        IEventSessionRepository eventSessionRepository,
        IEventRepository eventRepository,
        ILocationRepository locationRepository,
        IRegistrationModeRepository registrationModeRepository,
        IEventSessionKindRepository eventSessionKindRepository,
        IEventSessionIslamicAspectRepository eventSessionIslamicAspectRepository,
        IEventScheduleProjectionCalculator scheduleProjectionCalculator,
        IEventDayRepository eventDayRepository,
        IMapper mapper)
    {
        _eventSessionRepository = eventSessionRepository;
        _eventRepository = eventRepository;
        _locationRepository = locationRepository;
        _registrationModeRepository = registrationModeRepository;
        _eventSessionKindRepository = eventSessionKindRepository;
        _eventSessionIslamicAspectRepository = eventSessionIslamicAspectRepository;
        _scheduleProjectionCalculator = scheduleProjectionCalculator;
        _eventDayRepository = eventDayRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventSessionCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateEventSessionDtoValidator(
            _eventRepository,
            _locationRepository,
            _registrationModeRepository,
            _eventSessionKindRepository,
            _eventSessionRepository);
        var validationResult = await validator.ValidateAsync(request.EventSessionDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event session update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var eventSession = await _eventSessionRepository.GetById(request.EventSessionDto.Id);

        if (eventSession == null)
        {
            response.Success = false;
            response.Message = "Event session not found.";
            return response;
        }

        // Verify the target event belongs to the same tenant as the session (defense-in-depth)
        var parentEvent = await _eventRepository.GetById(request.EventSessionDto.EventId);
        if (parentEvent == null || parentEvent.TenantId != eventSession.TenantId)
        {
            response.Success = false;
            response.Message = "Event does not belong to the same tenant as the session.";
            return response;
        }

        _mapper.Map(request.EventSessionDto, eventSession);

        // Populate cached local projection fields via the aggregate method that consumes the calculator.
        // Handlers never touch LocalStart*/LocalEnd* directly.
        eventSession.Reschedule(
            request.EventSessionDto.StartTime,
            request.EventSessionDto.EndTime,
            parentEvent.EventTimeZoneId ?? parentEvent.Timezone ?? string.Empty,
            _scheduleProjectionCalculator);

        // Auto-link to the matching EventDay by (EventId, LocalStartDate).
        // When rescheduled to a different date, the link moves to the new day (or null if no day exists).
        var matchingDay = await _eventDayRepository.FindByEventAndLocalDateAsync(
            parentEvent.Id, eventSession.LocalStartDate, cancellationToken);
        eventSession.EventDayId = matchingDay?.Id;

        try
        {
            // Layer B: serializable re-check of same-room overlap runs inside the repository guard method.
            // The entity's ConcurrencyStamp (IsConcurrencyToken) handles the same-row stale-write case.
            await _eventSessionRepository.UpdateWithRoomOverlapGuardAsync(eventSession, cancellationToken);
        }
        catch (RoomScheduleConflictException ex)
        {
            response.Success = false;
            response.Message = "Event session update failed.";
            response.Errors = new List<string> { ex.Message };
            response.FailureCode = "room_schedule_conflict";
            return response;
        }

        var existingIslamicAspect = await _eventSessionIslamicAspectRepository.GetById(eventSession.Id);
        if (request.EventSessionDto.IslamicAspect == null)
        {
            if (existingIslamicAspect != null)
            {
                await _eventSessionIslamicAspectRepository.Delete(existingIslamicAspect);
            }
        }
        else if (existingIslamicAspect == null)
        {
            var newAspect = _mapper.Map<EventSessionIslamicAspect>(request.EventSessionDto.IslamicAspect);
            newAspect.EventSessionId = eventSession.Id;
            newAspect.EventSession = null;
            await _eventSessionIslamicAspectRepository.Create(newAspect);
        }
        else
        {
            _mapper.Map(request.EventSessionDto.IslamicAspect, existingIslamicAspect);
            await _eventSessionIslamicAspectRepository.Update(existingIslamicAspect);
        }

        response.Success = true;
        response.Id = eventSession.Id;
        response.Message = "Event session updated successfully.";

        return response;
    }
}

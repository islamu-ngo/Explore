// ABOUTME: Handler for creating a new event session with validation and optional template instantiation.
// ABOUTME: Validates input, maps DTO, sets defaults, persists via repository, instantiates session custom properties from template.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventSession;
using Explore.Application.DTOs.EventSession.Validators;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventSessions.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Services.Scheduling;
using MediatR;

namespace Explore.Application.Features.EventSessions.Handlers.Commands;

public class CreateEventSessionCommandHandler : IRequestHandler<CreateEventSessionCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly IRegistrationModeRepository _registrationModeRepository;
    private readonly IEventSessionKindRepository _eventSessionKindRepository;
    private readonly IEventSessionIslamicAspectRepository _eventSessionIslamicAspectRepository;
    private readonly IEventSessionTemplateRepository _eventSessionTemplateRepository;
    private readonly IEventSessionCustomPropertyRepository _eventSessionCustomPropertyRepository;
    private readonly IEventSessionCustomPropertyProjectionUpdater _projectionUpdater;
    private readonly IEventSessionTemplateInstantiationService _instantiationService;
    private readonly IEventScheduleProjectionCalculator _scheduleProjectionCalculator;
    private readonly IEventDayRepository _eventDayRepository;
    private readonly IMapper _mapper;

    public CreateEventSessionCommandHandler(
        IEventSessionRepository eventSessionRepository,
        IEventRepository eventRepository,
        ILocationRepository locationRepository,
        IRegistrationModeRepository registrationModeRepository,
        IEventSessionKindRepository eventSessionKindRepository,
        IEventSessionIslamicAspectRepository eventSessionIslamicAspectRepository,
        IEventSessionTemplateRepository eventSessionTemplateRepository,
        IEventSessionCustomPropertyRepository eventSessionCustomPropertyRepository,
        IEventSessionCustomPropertyProjectionUpdater projectionUpdater,
        IEventSessionTemplateInstantiationService instantiationService,
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
        _eventSessionTemplateRepository = eventSessionTemplateRepository;
        _eventSessionCustomPropertyRepository = eventSessionCustomPropertyRepository;
        _projectionUpdater = projectionUpdater;
        _instantiationService = instantiationService;
        _scheduleProjectionCalculator = scheduleProjectionCalculator;
        _eventDayRepository = eventDayRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventSessionCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new CreateEventSessionDtoValidator(
            _eventRepository,
            _locationRepository,
            _registrationModeRepository,
            _eventSessionKindRepository,
            _eventSessionTemplateRepository,
            _eventSessionRepository);
        var validationResult = await validator.ValidateAsync(request.EventSessionDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event session creation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        // Fetch the parent event to inherit its TenantId (defense-in-depth: global query filter already scopes by tenant)
        var parentEvent = await _eventRepository.GetById(request.EventSessionDto.EventId);
        if (parentEvent == null)
        {
            response.Success = false;
            response.Message = "Event not found in the current tenant.";
            return response;
        }

        var eventSession = _mapper.Map<EventSession>(request.EventSessionDto);
        eventSession.CurrentAudienceAttendees = 0;
        eventSession.TenantId = parentEvent.TenantId;

        // Populate cached local projection fields via the single authorized write path on EventSession.
        // Handlers never touch LocalStart*/LocalEnd* directly; the aggregate method consumes the calculator.
        eventSession.Reschedule(
            request.EventSessionDto.StartTime,
            request.EventSessionDto.EndTime,
            parentEvent.EventTimeZoneId ?? parentEvent.Timezone ?? string.Empty,
            _scheduleProjectionCalculator);

        // Auto-link to the matching EventDay by (EventId, LocalStartDate).
        // Returns null when no EventDay exists for this date — EventDayId stays nullable during transition.
        var matchingDay = await _eventDayRepository.FindByEventAndLocalDateAsync(
            parentEvent.Id, eventSession.LocalStartDate, cancellationToken);
        eventSession.EventDayId = matchingDay?.Id;

        try
        {
            // Layer B: serializable re-check of same-room overlap runs inside the repository guard method.
            eventSession = await _eventSessionRepository.CreateWithRoomOverlapGuardAsync(eventSession, cancellationToken);
        }
        catch (RoomScheduleConflictException ex)
        {
            response.Success = false;
            response.Message = "Event session creation failed.";
            response.Errors = new List<string> { ex.Message };
            response.FailureCode = "room_schedule_conflict";
            return response;
        }

        if (request.EventSessionDto.IslamicAspect != null)
        {
            var islamicAspect = new EventSessionIslamicAspect();
            islamicAspect.EventSessionId = eventSession.Id;
            islamicAspect.EventSession = null;
            ApplyIslamicAspectDto(islamicAspect, request.EventSessionDto.IslamicAspect);

            await _eventSessionIslamicAspectRepository.Create(islamicAspect);
        }

        // Template instantiation: copy session template definitions to runtime
        if (request.EventSessionDto.SessionTemplateId.HasValue)
        {
            var sessionTemplate = await _eventSessionTemplateRepository.GetSessionTemplateWithDetails(
                request.EventSessionDto.SessionTemplateId.Value);

            if (sessionTemplate is { IsPublished: true, IsActive: true })
            {
                eventSession.SourceTemplateId = sessionTemplate.Id;
                eventSession.SourceTemplateKey = sessionTemplate.SessionTemplateKey;
                eventSession.SourceTemplateVersion = sessionTemplate.Version;
                eventSession.InstantiatedFromTemplateAt = DateTimeOffset.UtcNow;
                eventSession.LastSyncedFromTemplateAt = DateTimeOffset.UtcNow;
                await _eventSessionRepository.Update(eventSession);

                var instantiationResult = _instantiationService.InstantiateFromSessionTemplate(
                    eventSession.Id,
                    parentEvent.TenantId,
                    sessionTemplate,
                    "system");

                foreach (var runtimeDef in instantiationResult.Definitions)
                {
                    // Clear DefaultOptionId before initial save to avoid FK violation
                    runtimeDef.Definition.DefaultOptionId = null;

                    await _eventSessionCustomPropertyRepository.CreateWithOptions(
                        runtimeDef.Definition,
                        runtimeDef.Options,
                        runtimeDef.DefaultOptionId,
                        cancellationToken);

                    if (runtimeDef.DefaultValue != null)
                    {
                        await _eventSessionCustomPropertyRepository.SetValue(
                            runtimeDef.DefaultValue, cancellationToken);
                    }
                }

                await _projectionUpdater.RefreshForEventSessionAsync(eventSession.Id, cancellationToken);
            }
        }

        response.Success = true;
        response.Id = eventSession.Id;
        response.Message = "Event session created successfully.";

        return response;
    }

    private static void ApplyIslamicAspectDto(
        EventSessionIslamicAspect aspect,
        EventSessionIslamicAspectDto dto)
    {
        aspect.ApplyScheduling(dto.StartTimeType, dto.ReferencePrayer, dto.OffsetMinutes);
        aspect.RequiresWudu = dto.RequiresWudu;
        aspect.RitualRequirementsJson = dto.RitualRequirementsJson;
    }
}

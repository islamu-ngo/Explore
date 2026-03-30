// ABOUTME: Handler for creating a new event session with validation and optional template instantiation.
// ABOUTME: Validates input, maps DTO, sets defaults, persists via repository, instantiates session custom properties from template.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventSession.Validators;
using Explore.Application.Features.EventSessions.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.EventSessions.Handlers.Commands;

public class CreateEventSessionCommandHandler : IRequestHandler<CreateEventSessionCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly IRegistrationModeRepository _registrationModeRepository;
    private readonly IEventSessionIslamicAspectRepository _eventSessionIslamicAspectRepository;
    private readonly IEventSessionTemplateRepository _eventSessionTemplateRepository;
    private readonly IEventSessionCustomPropertyRepository _eventSessionCustomPropertyRepository;
    private readonly IEventSessionTemplateInstantiationService _instantiationService;
    private readonly IMapper _mapper;

    public CreateEventSessionCommandHandler(
        IEventSessionRepository eventSessionRepository,
        IEventRepository eventRepository,
        ILocationRepository locationRepository,
        IRegistrationModeRepository registrationModeRepository,
        IEventSessionIslamicAspectRepository eventSessionIslamicAspectRepository,
        IEventSessionTemplateRepository eventSessionTemplateRepository,
        IEventSessionCustomPropertyRepository eventSessionCustomPropertyRepository,
        IEventSessionTemplateInstantiationService instantiationService,
        IMapper mapper)
    {
        _eventSessionRepository = eventSessionRepository;
        _eventRepository = eventRepository;
        _locationRepository = locationRepository;
        _registrationModeRepository = registrationModeRepository;
        _eventSessionIslamicAspectRepository = eventSessionIslamicAspectRepository;
        _eventSessionTemplateRepository = eventSessionTemplateRepository;
        _eventSessionCustomPropertyRepository = eventSessionCustomPropertyRepository;
        _instantiationService = instantiationService;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventSessionCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new CreateEventSessionDtoValidator(_eventRepository, _locationRepository, _registrationModeRepository, _eventSessionTemplateRepository);
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

        eventSession = await _eventSessionRepository.Create(eventSession);

        if (request.EventSessionDto.IslamicAspect != null)
        {
            var islamicAspect = _mapper.Map<EventSessionIslamicAspect>(request.EventSessionDto.IslamicAspect);
            islamicAspect.EventSessionId = eventSession.Id;
            islamicAspect.EventSession = null;

            await _eventSessionIslamicAspectRepository.Create(islamicAspect);
        }

        // Template instantiation: copy session template definitions to runtime
        if (request.EventSessionDto.SessionTemplateId.HasValue)
        {
            var sessionTemplate = await _eventSessionTemplateRepository.GetSessionTemplateWithDetails(
                request.EventSessionDto.SessionTemplateId.Value);

            if (sessionTemplate is { IsPublished: true, IsActive: true })
            {
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
            }
        }

        response.Success = true;
        response.Id = eventSession.Id;
        response.Message = "Event session created successfully.";

        return response;
    }
}

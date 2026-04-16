// ABOUTME: Handler for creating an event together with its initial sessions in one operation.
// ABOUTME: Orchestrates event + session creation atomically via UoW.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Event.Validators;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Services;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.Events.Handlers.Commands;

public class CreateEventWithSessionsCommandHandler : IRequestHandler<CreateEventWithSessionsCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IEventSessionIslamicAspectRepository _eventSessionIslamicAspectRepository;
    private readonly IEventSessionLanguageRepository _eventSessionLanguageRepository;
    private readonly IEventActorResolver _actorResolver;
    private readonly IAudienceAgeRepository _audienceAgeRepository;
    private readonly IAudienceGenderRepository _audienceGenderRepository;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly IRegistrationModeRepository _registrationModeRepository;
    private readonly ILanguageRepository _languageRepository;
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public CreateEventWithSessionsCommandHandler(
        IEventRepository eventRepository,
        IEventSessionRepository eventSessionRepository,
        IEventSessionIslamicAspectRepository eventSessionIslamicAspectRepository,
        IEventSessionLanguageRepository eventSessionLanguageRepository,
        IEventActorResolver actorResolver,
        IAudienceAgeRepository audienceAgeRepository,
        IAudienceGenderRepository audienceGenderRepository,
        IEventTypeRepository eventTypeRepository,
        IStorageObjectRepository storageObjectRepository,
        IOrganizationRepository organizationRepository,
        IGroupRepository groupRepository,
        ILocationRepository locationRepository,
        IRegistrationModeRepository registrationModeRepository,
        ILanguageRepository languageRepository,
        IUserContext userContext,
        ITenantContext tenantContext,
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _eventSessionRepository = eventSessionRepository;
        _eventSessionIslamicAspectRepository = eventSessionIslamicAspectRepository;
        _eventSessionLanguageRepository = eventSessionLanguageRepository;
        _actorResolver = actorResolver;
        _audienceAgeRepository = audienceAgeRepository;
        _audienceGenderRepository = audienceGenderRepository;
        _eventTypeRepository = eventTypeRepository;
        _storageObjectRepository = storageObjectRepository;
        _organizationRepository = organizationRepository;
        _groupRepository = groupRepository;
        _locationRepository = locationRepository;
        _registrationModeRepository = registrationModeRepository;
        _languageRepository = languageRepository;
        _userContext = userContext;
        _tenantContext = tenantContext;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventWithSessionsCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var currentUserId = _userContext.GetRequiredUserId();

        var validator = new CreateEventWithSessionsDtoValidator(
            _audienceAgeRepository,
            _audienceGenderRepository,
            _eventTypeRepository,
            _organizationRepository,
            _groupRepository,
            _storageObjectRepository,
            _locationRepository,
            _registrationModeRepository,
            _languageRepository);

        var validationResult = await validator.ValidateAsync(request.EventWithSessionsDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event creation failed due to validation errors.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var dto = request.EventWithSessionsDto;

        var actorResult = await _actorResolver.ResolveAsync(
            currentUserId, dto.OrganizationId, dto.GroupId, cancellationToken);

        if (!actorResult.Succeeded)
        {
            response.Success = false;
            response.Message = actorResult.ErrorMessage!;
            response.Errors = new List<string> { actorResult.ErrorDetail! };
            return response;
        }

        var sessions = dto.Sessions;
        var firstSessionDate = sessions.Min(s => s.StartTime);
        var lastSessionDate = sessions.Max(s => s.EndTime);

        var firstSessionDateOnly = DateOnly.FromDateTime(firstSessionDate.UtcDateTime);
        var lastSessionDateOnly = DateOnly.FromDateTime(lastSessionDate.UtcDateTime);

        var @event = new Event
        {
            Title = dto.Title,
            Description = dto.Description,
            Slug = string.IsNullOrWhiteSpace(dto.Slug) ? SlugGenerator.FromTitle(dto.Title, "event") : dto.Slug,
            EventTypeId = dto.EventTypeId,
            AudienceGenderId = dto.AudienceGenderId,
            AudienceAgeId = dto.AudienceAgeId,
            Price = dto.Price,
            CurrencyCode = dto.CurrencyCode,
            FeaturedImageId = dto.FeaturedImageId,
            IsRegistrationRequired = dto.IsRegistrationRequired,
            ExternalRegistrationUrl = dto.ExternalRegistrationUrl,
            EventStatusId = dto.EventStatusId == 0 ? 1 : dto.EventStatusId,
            VisibilityTypeId = dto.VisibilityTypeId == 0 ? 1 : dto.VisibilityTypeId,
            EventFormatId = dto.EventFormatId == 0 ? 1 : dto.EventFormatId,
            MadhabId = dto.MadhabId,
            Timezone = dto.Timezone,
            EventUrl = dto.EventUrl,
            ActorId = actorResult.ActorId,
            Actor = null!,
            TenantId = _tenantContext.TenantId,
            Tenant = null!,
            TotalViews = 0,
            IsUserReported = actorResult.IsUserReported,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!,
            FirstSessionDate = firstSessionDateOnly,
            LastSessionDate = lastSessionDateOnly,
            SessionCount = sessions.Count
        };

        var eventId = await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            @event = await _eventRepository.Create(@event);

            if (dto.FeaturedImageId.HasValue)
            {
                var storageObject = await _storageObjectRepository.GetById(dto.FeaturedImageId.Value);
                if (storageObject != null)
                {
                    storageObject.ActorId = actorResult.ActorId;
                    await _storageObjectRepository.Update(storageObject);
                }
            }

            var sessionIndex = 0;
            foreach (var sessionDto in sessions)
            {
                sessionIndex++;

                var eventSession = new EventSession
                {
                    EventId = @event.Id,
                    Event = null!,
                    TenantId = _tenantContext.TenantId,
                    Tenant = null!,
                    Title = string.IsNullOrWhiteSpace(sessionDto.Title) ? @event.Title : sessionDto.Title,
                    Description = sessionDto.Description,
                    StartTime = sessionDto.StartTime,
                    EndTime = sessionDto.EndTime,
                    LocationId = sessionDto.LocationId,
                    MaxAudienceAttendees = sessionDto.MaxAudienceAttendees,
                    CurrentAudienceAttendees = 0,
                    RegistrationModeId = sessionDto.RegistrationModeId ?? (dto.IsRegistrationRequired ? 1 : null),
                    Price = sessionDto.Price,
                    CurrencyCode = sessionDto.CurrencyCode,
                    Slug = SlugGenerator.FromTitle(
                        string.IsNullOrWhiteSpace(sessionDto.Title)
                            ? $"{@event.Title}-session-{sessionIndex}"
                            : sessionDto.Title,
                        "session")
                };

                eventSession = await _eventSessionRepository.Create(eventSession);

                if (sessionDto.IslamicAspect != null)
                {
                    var islamicAspect = new EventSessionIslamicAspect
                    {
                        EventSessionId = eventSession.Id,
                        StartTimeType = sessionDto.IslamicAspect.StartTimeType,
                        ReferencePrayer = sessionDto.IslamicAspect.ReferencePrayer,
                        OffsetMinutes = sessionDto.IslamicAspect.OffsetMinutes,
                        RequiresWudu = sessionDto.IslamicAspect.RequiresWudu,
                        RitualRequirementsJson = sessionDto.IslamicAspect.RitualRequirementsJson
                    };

                    await _eventSessionIslamicAspectRepository.Create(islamicAspect);
                }

                foreach (var languageId in sessionDto.LanguageIds)
                {
                    var sessionLanguage = new EventSessionLanguage
                    {
                        EventSessionId = eventSession.Id,
                        EventSession = null!,
                        LanguageId = languageId,
                        Language = null!,
                        TenantId = _tenantContext.TenantId,
                        Tenant = null!
                    };

                    await _eventSessionLanguageRepository.Create(sessionLanguage);
                }
            }

            return @event.Id;
        }, cancellationToken);

        response.Success = true;
        response.Id = eventId;
        response.Message = $"Event and {sessions.Count} session(s) created successfully.";

        return response;
    }
}

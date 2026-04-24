// ABOUTME: Handler for creating a new event with full validation.
// ABOUTME: Validates input, resolves actor, maps DTO, persists event + default session via UoW.
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
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.Event.Validators;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Services;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Services.Scheduling;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.Events.Handlers.Commands;

public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IEventActorResolver _actorResolver;
    private readonly IAudienceAgeRepository _audienceAgeRepository;
    private readonly IAudienceGenderRepository _audienceGenderRepository;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IEventTemplateRepository _eventTemplateRepository;
    private readonly IEventSeriesRepository _eventSeriesRepository;
    private readonly IEventRegistrationPolicyRepository _eventRegistrationPolicyRepository;
    private readonly IEventCustomPropertyRepository _eventCustomPropertyRepository;
    private readonly IEventCustomPropertyProjectionUpdater _projectionUpdater;
    private readonly IEventTemplateInstantiationService _instantiationService;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IEventDayRepository _eventDayRepository;
    private readonly ILocationRoomRepository _locationRoomRepository;
    private readonly IEventAgendaItemRepository _eventAgendaItemRepository;
    private readonly IEventScheduleProjectionCalculator _scheduleProjectionCalculator;
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;
    private readonly BusinessMetrics _metrics;
    private readonly IUnitOfWork _unitOfWork;

    public CreateEventCommandHandler(
        IEventRepository eventRepository,
        IEventSessionRepository eventSessionRepository,
        IEventActorResolver actorResolver,
        IAudienceAgeRepository audienceAgeRepository,
        IAudienceGenderRepository audienceGenderRepository,
        IEventTypeRepository eventTypeRepository,
        IStorageObjectRepository storageObjectRepository,
        IEventTemplateRepository eventTemplateRepository,
        IEventSeriesRepository eventSeriesRepository,
        IEventRegistrationPolicyRepository eventRegistrationPolicyRepository,
        IEventCustomPropertyRepository eventCustomPropertyRepository,
        IEventCustomPropertyProjectionUpdater projectionUpdater,
        IEventTemplateInstantiationService instantiationService,
        IOrganizationRepository organizationRepository,
        IGroupRepository groupRepository,
        IEventDayRepository eventDayRepository,
        ILocationRoomRepository locationRoomRepository,
        IEventAgendaItemRepository eventAgendaItemRepository,
        IEventScheduleProjectionCalculator scheduleProjectionCalculator,
        IUserContext userContext,
        ITenantContext tenantContext,
        IMapper mapper,
        HybridCache cache,
        BusinessMetrics metrics,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _eventSessionRepository = eventSessionRepository;
        _actorResolver = actorResolver;
        _audienceAgeRepository = audienceAgeRepository;
        _audienceGenderRepository = audienceGenderRepository;
        _eventTypeRepository = eventTypeRepository;
        _storageObjectRepository = storageObjectRepository;
        _eventTemplateRepository = eventTemplateRepository;
        _eventSeriesRepository = eventSeriesRepository;
        _eventRegistrationPolicyRepository = eventRegistrationPolicyRepository;
        _eventCustomPropertyRepository = eventCustomPropertyRepository;
        _projectionUpdater = projectionUpdater;
        _instantiationService = instantiationService;
        _organizationRepository = organizationRepository;
        _groupRepository = groupRepository;
        _eventDayRepository = eventDayRepository;
        _locationRoomRepository = locationRoomRepository;
        _eventAgendaItemRepository = eventAgendaItemRepository;
        _scheduleProjectionCalculator = scheduleProjectionCalculator;
        _userContext = userContext;
        _tenantContext = tenantContext;
        _mapper = mapper;
        _cache = cache;
        _metrics = metrics;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var currentUserId = _userContext.GetRequiredUserId();

        var validator = new CreateEventDtoValidator(
            _audienceAgeRepository,
            _audienceGenderRepository,
            _eventTypeRepository,
            _organizationRepository,
            _groupRepository,
            _storageObjectRepository,
            _eventTemplateRepository,
            _eventSeriesRepository,
            _eventRegistrationPolicyRepository);

        var validationResult = await validator.ValidateAsync(request.EventDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event creation failed due to validation errors.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var actorResult = await _actorResolver.ResolveAsync(
            currentUserId, request.EventDto.OrganizationId, request.EventDto.GroupId, cancellationToken);

        if (!actorResult.Succeeded)
        {
            response.Success = false;
            response.Message = actorResult.ErrorMessage!;
            response.Errors = new List<string> { actorResult.ErrorDetail! };
            return response;
        }

        var @event = _mapper.Map<Event>(request.EventDto);
        @event.ActorId = actorResult.ActorId;
        @event.TotalViews = 0;
        @event.TenantId = _tenantContext.TenantId;
        @event.IsUserReported = actorResult.IsUserReported;
        if (@event.EventStatusId == 0) @event.EventStatusId = 1;
        if (@event.VisibilityTypeId == 0) @event.VisibilityTypeId = 1;
        if (@event.EventFormatId == 0) @event.EventFormatId = 1;

        var eventId = await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            @event = await _eventRepository.Create(@event);

            if (request.EventDto.FeaturedImageId.HasValue)
            {
                var storageObject = await _storageObjectRepository.GetById(request.EventDto.FeaturedImageId.Value);
                if (storageObject != null)
                {
                    storageObject.ActorId = actorResult.ActorId;
                    await _storageObjectRepository.Update(storageObject);
                }
            }

            var eventSession = new EventSession
            {
                EventId = @event.Id,
                Event = null!,
                TenantId = _tenantContext.TenantId,
                Tenant = null!,
                Title = @event.Title,
                Description = @event.Description,
                StartTime = request.EventDto.FirstSessionDate ?? DateTimeOffset.UtcNow,
                EndTime = request.EventDto.LastSessionDate ?? DateTimeOffset.UtcNow.AddHours(2),
                LocationId = null,
                MaxAudienceAttendees = null,
                CurrentAudienceAttendees = 0,
                RegistrationModeId = request.EventDto.IsRegistrationRequired ? 1 : null,
                Slug = SlugGenerator.FromTitle(@event.Title, "session")
            };

            await _eventSessionRepository.Create(eventSession);

            if (request.EventDto.TemplateId.HasValue)
            {
                var template = await _eventTemplateRepository.GetTemplateWithDetails(request.EventDto.TemplateId.Value);
                if (template is { IsPublished: true, IsActive: true })
                {
                    @event.SourceTemplateId = template.Id;
                    @event.SourceTemplateKey = template.TemplateKey;
                    @event.SourceTemplateVersion = template.Version;
                    @event.InstantiatedFromTemplateAt = DateTimeOffset.UtcNow;
                    @event.LastSyncedFromTemplateAt = DateTimeOffset.UtcNow;
                    await _eventRepository.Update(@event);

                    var instantiationResult = _instantiationService.InstantiateFromTemplate(
                        @event.Id, _tenantContext.TenantId, template, currentUserId.ToString());

                    foreach (var defWithOptions in instantiationResult.Definitions)
                    {
                        defWithOptions.Definition.DefaultOptionId = null;

                        await _eventCustomPropertyRepository.CreateWithOptions(
                            defWithOptions.Definition,
                            defWithOptions.Options,
                            defWithOptions.DefaultOptionId,
                            ct);

                        if (defWithOptions.DefaultValue != null)
                        {
                            await _eventCustomPropertyRepository.SetValue(defWithOptions.DefaultValue, ct);
                        }
                    }

                    await _projectionUpdater.RefreshForEventAsync(@event.Id, ct);
                }
            }

            await CreateInlineSchedulingAsync(request.EventDto, @event, ct);

            return @event.Id;
        }, cancellationToken);

        response.Success = true;
        response.Id = eventId;
        response.Message = "Event and session created successfully.";

        _metrics.RecordEventCreated(_tenantContext.TenantId.ToString());
        await _cache.RemoveAsync($"event:detail:{eventId}", cancellationToken);
        await _cache.RemoveAsync("events:list:1:20", cancellationToken);

        return response;
    }

    private async Task CreateInlineSchedulingAsync(CreateEventDto dto, Event @event, CancellationToken ct)
    {
        var createdDays = new List<EventDay>();

        if (dto.Days is { Count: > 0 })
        {
            foreach (var dayDto in dto.Days)
            {
                var day = new EventDay
                {
                    EventId = @event.Id,
                    Event = null!,
                    TenantId = _tenantContext.TenantId,
                    Tenant = null!,
                    LocalDate = dayDto.LocalDate,
                    Label = dayDto.Label,
                    Description = dayDto.Description,
                    BannerText = dayDto.BannerText,
                    BannerImageId = dayDto.BannerImageId,
                    IsPublished = dayDto.IsPublished,
                    SortOrder = dayDto.SortOrder,
                    AllowsDayScopeRegistration = dayDto.AllowsDayScopeRegistration
                };
                day = await _eventDayRepository.Create(day);
                createdDays.Add(day);
            }
        }

        if (dto.Rooms is { Count: > 0 })
        {
            foreach (var roomDto in dto.Rooms)
            {
                var room = new LocationRoom
                {
                    LocationId = roomDto.LocationId,
                    Location = null!,
                    TenantId = _tenantContext.TenantId,
                    Tenant = null!,
                    Name = roomDto.Name,
                    Slug = roomDto.Slug,
                    Description = roomDto.Description,
                    Capacity = roomDto.Capacity,
                    SortOrder = roomDto.SortOrder
                };
                await _locationRoomRepository.Create(room);
            }
        }

        if (dto.AgendaItems is { Count: > 0 })
        {
            var timezoneId = @event.EventTimeZoneId ?? @event.Timezone ?? string.Empty;

            foreach (var itemDto in dto.AgendaItems)
            {
                var agendaItem = new EventAgendaItem
                {
                    EventId = @event.Id,
                    Event = null!,
                    TenantId = _tenantContext.TenantId,
                    Tenant = null!,
                    Title = itemDto.Title,
                    Description = itemDto.Description,
                    RoomId = itemDto.RoomId,
                    KindId = itemDto.KindId,
                    SortOrder = itemDto.SortOrder,
                    StartTime = itemDto.StartTime,
                    EndTime = itemDto.EndTime
                };

                agendaItem.Reschedule(itemDto.StartTime, itemDto.EndTime, timezoneId, _scheduleProjectionCalculator);

                var matchingDay = createdDays.Find(d => d.LocalDate == agendaItem.LocalStartDate);
                agendaItem.EventDayId = matchingDay?.Id;

                await _eventAgendaItemRepository.Create(agendaItem);
            }
        }
    }
}

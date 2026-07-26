// ABOUTME: Handler for the canonical single-submit Create Event graph command.
// ABOUTME: Validates, resolves publisher ownership, persists event graph atomically, and creates initial EventOwner role assignment.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Caching;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.Event.Validators;
using Explore.Application.Exceptions;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Features.Federation.Atproto.Services;
using Explore.Application.Responses;
using Explore.Application.Services;
using Explore.Application.Services.Lifecycle;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Federation;
using Explore.Domain.Services.Scheduling;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.Events.Handlers.Commands;

public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IEventSessionSpeakerRepository _eventSessionSpeakerRepository;
    private readonly IEventIslamicAspectRepository _eventIslamicAspectRepository;
    private readonly IEventSessionIslamicAspectRepository _eventSessionIslamicAspectRepository;
    private readonly IEventSessionLanguageRepository _eventSessionLanguageRepository;
    private readonly IEventRoleAssignmentRepository _eventRoleAssignmentRepository;
    private readonly IEventActorResolver _actorResolver;
    private readonly IAudienceAgeRepository _audienceAgeRepository;
    private readonly IAudienceGenderRepository _audienceGenderRepository;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IEventTemplateRepository _eventTemplateRepository;
    private readonly IEventSeriesRepository _eventSeriesRepository;
    private readonly IEventRegistrationPolicyRepository _eventRegistrationPolicyRepository;
    private readonly IEventCustomPropertyRepository _eventCustomPropertyRepository;
    private readonly IEventCustomPropertyProjectionUpdater _eventCustomPropertyProjectionUpdater;
    private readonly IEventTemplateInstantiationService _eventTemplateInstantiationService;
    private readonly IEventSessionTemplateRepository _eventSessionTemplateRepository;
    private readonly IEventSessionCustomPropertyRepository _eventSessionCustomPropertyRepository;
    private readonly IEventSessionCustomPropertyProjectionUpdater _eventSessionCustomPropertyProjectionUpdater;
    private readonly IEventSessionTemplateInstantiationService _eventSessionTemplateInstantiationService;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly IRegistrationModeRepository _registrationModeRepository;
    private readonly ILanguageRepository _languageRepository;
    private readonly IMadhabRepository _madhabRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IScheduleItemKindRepository _scheduleItemKindRepository;
    private readonly IEventSessionKindRepository _eventSessionKindRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IEventDayRepository _eventDayRepository;
    private readonly ILocationRoomRepository _locationRoomRepository;
    private readonly IEventAgendaItemRepository _eventAgendaItemRepository;
    private readonly IEventCategoriesRepository _eventCategoriesRepository;
    private readonly IEventTagsRepository _eventTagsRepository;
    private readonly IEventScheduleProjectionCalculator _scheduleProjectionCalculator;
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;
    private readonly HybridCache _cache;
    private readonly BusinessMetrics _metrics;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IEventLifecyclePolicyProvider _lifecyclePolicyProvider;
    private readonly IEventLifecycleReadinessEvaluator _lifecycleReadinessEvaluator;
    private readonly EventLocationAttachmentService _eventLocationAttachmentService;
    private readonly AtprotoEventPublicationPlanner _atprotoPublicationPlanner;

    public CreateEventCommandHandler(
        IEventRepository eventRepository,
        IEventSessionRepository eventSessionRepository,
        IEventSessionSpeakerRepository eventSessionSpeakerRepository,
        IEventIslamicAspectRepository eventIslamicAspectRepository,
        IEventSessionIslamicAspectRepository eventSessionIslamicAspectRepository,
        IEventSessionLanguageRepository eventSessionLanguageRepository,
        IEventRoleAssignmentRepository eventRoleAssignmentRepository,
        IEventActorResolver actorResolver,
        IAudienceAgeRepository audienceAgeRepository,
        IAudienceGenderRepository audienceGenderRepository,
        IEventTypeRepository eventTypeRepository,
        IStorageObjectRepository storageObjectRepository,
        IEventTemplateRepository eventTemplateRepository,
        IEventSeriesRepository eventSeriesRepository,
        IEventRegistrationPolicyRepository eventRegistrationPolicyRepository,
        IEventCustomPropertyRepository eventCustomPropertyRepository,
        IEventCustomPropertyProjectionUpdater eventCustomPropertyProjectionUpdater,
        IEventTemplateInstantiationService eventTemplateInstantiationService,
        IEventSessionTemplateRepository eventSessionTemplateRepository,
        IEventSessionCustomPropertyRepository eventSessionCustomPropertyRepository,
        IEventSessionCustomPropertyProjectionUpdater eventSessionCustomPropertyProjectionUpdater,
        IEventSessionTemplateInstantiationService eventSessionTemplateInstantiationService,
        IOrganizationRepository organizationRepository,
        IGroupRepository groupRepository,
        ILocationRepository locationRepository,
        IRegistrationModeRepository registrationModeRepository,
        ILanguageRepository languageRepository,
        IMadhabRepository madhabRepository,
        ICategoryRepository categoryRepository,
        ITagRepository tagRepository,
        IScheduleItemKindRepository scheduleItemKindRepository,
        IEventSessionKindRepository eventSessionKindRepository,
        IActorRepository actorRepository,
        IEventDayRepository eventDayRepository,
        ILocationRoomRepository locationRoomRepository,
        IEventAgendaItemRepository eventAgendaItemRepository,
        IEventCategoriesRepository eventCategoriesRepository,
        IEventTagsRepository eventTagsRepository,
        IEventScheduleProjectionCalculator scheduleProjectionCalculator,
        IUserContext userContext,
        ITenantContext tenantContext,
        HybridCache cache,
        BusinessMetrics metrics,
        IUnitOfWork unitOfWork,
        IOutboxRepository outboxRepository,
        IEventLifecyclePolicyProvider lifecyclePolicyProvider,
        IEventLifecycleReadinessEvaluator lifecycleReadinessEvaluator,
        EventLocationAttachmentService eventLocationAttachmentService,
        AtprotoEventPublicationPlanner atprotoPublicationPlanner)
    {
        _eventRepository = eventRepository;
        _eventSessionRepository = eventSessionRepository;
        _eventSessionSpeakerRepository = eventSessionSpeakerRepository;
        _eventIslamicAspectRepository = eventIslamicAspectRepository;
        _eventSessionIslamicAspectRepository = eventSessionIslamicAspectRepository;
        _eventSessionLanguageRepository = eventSessionLanguageRepository;
        _eventRoleAssignmentRepository = eventRoleAssignmentRepository;
        _actorResolver = actorResolver;
        _audienceAgeRepository = audienceAgeRepository;
        _audienceGenderRepository = audienceGenderRepository;
        _eventTypeRepository = eventTypeRepository;
        _storageObjectRepository = storageObjectRepository;
        _eventTemplateRepository = eventTemplateRepository;
        _eventSeriesRepository = eventSeriesRepository;
        _eventRegistrationPolicyRepository = eventRegistrationPolicyRepository;
        _eventCustomPropertyRepository = eventCustomPropertyRepository;
        _eventCustomPropertyProjectionUpdater = eventCustomPropertyProjectionUpdater;
        _eventTemplateInstantiationService = eventTemplateInstantiationService;
        _eventSessionTemplateRepository = eventSessionTemplateRepository;
        _eventSessionCustomPropertyRepository = eventSessionCustomPropertyRepository;
        _eventSessionCustomPropertyProjectionUpdater = eventSessionCustomPropertyProjectionUpdater;
        _eventSessionTemplateInstantiationService = eventSessionTemplateInstantiationService;
        _organizationRepository = organizationRepository;
        _groupRepository = groupRepository;
        _locationRepository = locationRepository;
        _registrationModeRepository = registrationModeRepository;
        _languageRepository = languageRepository;
        _madhabRepository = madhabRepository;
        _categoryRepository = categoryRepository;
        _tagRepository = tagRepository;
        _scheduleItemKindRepository = scheduleItemKindRepository;
        _eventSessionKindRepository = eventSessionKindRepository;
        _actorRepository = actorRepository;
        _eventDayRepository = eventDayRepository;
        _locationRoomRepository = locationRoomRepository;
        _eventAgendaItemRepository = eventAgendaItemRepository;
        _eventCategoriesRepository = eventCategoriesRepository;
        _eventTagsRepository = eventTagsRepository;
        _scheduleProjectionCalculator = scheduleProjectionCalculator;
        _userContext = userContext;
        _tenantContext = tenantContext;
        _cache = cache;
        _metrics = metrics;
        _unitOfWork = unitOfWork;
        _outboxRepository = outboxRepository;
        _lifecyclePolicyProvider = lifecyclePolicyProvider;
        _lifecycleReadinessEvaluator = lifecycleReadinessEvaluator;
        _eventLocationAttachmentService = eventLocationAttachmentService;
        _atprotoPublicationPlanner = atprotoPublicationPlanner;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var dto = request.Request;
        var currentUserId = _userContext.GetRequiredUserId();

        var validationErrors = await ValidateRequestAsync(dto, cancellationToken);
        if (validationErrors.Count > 0)
        {
            response.Success = false;
            response.Message = "Event creation failed due to validation errors.";
            response.Errors = validationErrors;
            return response;
        }

        var actorResult = await ResolvePublisherActorAsync(dto, currentUserId, cancellationToken);
        if (!actorResult.Succeeded)
        {
            response.Success = false;
            response.Message = actorResult.ErrorMessage!;
            response.Errors = new List<string> { actorResult.ErrorDetail! };
            return response;
        }

        var timezoneId = ResolveTimezoneId(dto);
        var createdAt = DateTimeOffset.UtcNow;
        var eventEntity = BuildEventEntity(dto, actorResult, timezoneId, currentUserId, createdAt);
        var federationOutboxId = Guid.CreateVersion7();
        var federationCreatedAt = DateTime.UtcNow;

        if (eventEntity.EventStatusId == (int)EventStatusEnum.Published)
        {
            EventLifecyclePolicy policy = await _lifecyclePolicyProvider.GetEffectivePolicyAsync(eventEntity.TenantId, ValidationProfile.EventPublish, cancellationToken);
            LifecycleReadinessResult readiness = _lifecycleReadinessEvaluator.Evaluate(eventEntity, policy.Profile, policy);
            if (!readiness.IsReady)
            {
                response.Success = false;
                response.Message = "Event creation failed because the event is not ready to publish.";
                response.Errors = readiness.Errors.Select(error => error.Message).ToList();
                response.FailureCode = "event_publish_readiness_failed";
                return response;
            }
        }

        try
        {
            var eventId = await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                eventEntity = await _eventRepository.Create(eventEntity);
                await AssignFeaturedImageActorAsync(dto, actorResult.ActorId);
                await CreateEventIslamicAspectAsync(dto, eventEntity, ct);
                await AssignInitialEventOwnerAsync(eventEntity, currentUserId, ct);

                var locationMap = await CreateLocationsAsync(dto, ct);
                var dayMaps = await CreateEventDaysAsync(dto, eventEntity, timezoneId, ct);
                var roomMap = await CreateRoomsAsync(dto, locationMap, ct);
                await CreateSessionsAsync(dto, eventEntity, locationMap, dayMaps, roomMap, timezoneId, currentUserId, createdAt, ct);
                await CreateEventAgendaItemsAsync(dto, eventEntity, locationMap, dayMaps, roomMap, timezoneId, ct);
                await CreateCategoryAndTagAssignmentsAsync(dto, eventEntity, ct);
                await InstantiateTemplatePropertiesAsync(dto, eventEntity, currentUserId, createdAt, ct);

                if (eventEntity.EventStatusId == (int)EventStatusEnum.Published)
                {
                    await _atprotoPublicationPlanner.PlanEventAsync(
                        new AtprotoEventPublicationInput(
                            eventEntity.TenantId,
                            currentUserId,
                            eventEntity.Id,
                            eventEntity.ConcurrencyStamp,
                            PdsSyncOperation.Create,
                            federationOutboxId,
                            federationCreatedAt),
                        ct);
                    var publishedAt = DateTimeOffset.UtcNow;
                    await _outboxRepository.Create(EventPublishedOutboxMessageFactory.CreateNotificationFanoutOutboxMessage(eventEntity, publishedAt));
                }

                return eventEntity.Id;
            }, cancellationToken);

            response.Success = true;
            response.Id = eventId;
            response.Message = "Event created successfully.";

            _metrics.RecordEventCreated(_tenantContext.TenantId.ToString());
            try
            {
                await _cache.RemoveAsync($"event:detail:{eventId}", cancellationToken);
                await _cache.RemoveByTagAsync(CacheTags.EventListByTenant(_tenantContext.TenantId), cancellationToken);
            }
            catch (Exception)
            {
                // Best-effort cache invalidation - Redis may be unavailable in local dev
            }
        }
        catch (RoomScheduleConflictException ex)
        {
            response.Success = false;
            response.Message = "Event creation failed.";
            response.Errors = new List<string> { ex.Message };
            response.FailureCode = "room_schedule_conflict";
        }

        return response;
    }

    private async Task<List<string>> ValidateRequestAsync(CreateEventRequest request, CancellationToken cancellationToken)
    {
        var validator = new CreateEventRequestValidator(
            _audienceAgeRepository,
            _audienceGenderRepository,
            _eventTypeRepository,
            _organizationRepository,
            _groupRepository,
            _storageObjectRepository,
            _eventTemplateRepository,
            _eventSeriesRepository,
            _eventRegistrationPolicyRepository,
            _locationRepository,
            _registrationModeRepository,
            _languageRepository,
            _madhabRepository,
            _categoryRepository,
            _tagRepository,
            _scheduleItemKindRepository,
            _eventSessionKindRepository,
            _locationRoomRepository,
            _eventSessionTemplateRepository,
            _actorRepository);

        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        return validationResult.Errors.Select(e => e.ErrorMessage).ToList();
    }

    private Task<EventActorResult> ResolvePublisherActorAsync(CreateEventRequest request, Guid currentUserId, CancellationToken cancellationToken) =>
        _actorResolver.ResolveAsync(currentUserId, request.OrganizationId, request.GroupId, cancellationToken);

    private Event BuildEventEntity(
        CreateEventRequest dto,
        EventActorResult actorResult,
        string timezoneId,
        Guid currentUserId,
        DateTimeOffset createdAt)
    {
        var eventStatusId = dto.EventStatusId == 0 ? (int)EventStatusEnum.Draft : dto.EventStatusId;
        var publicSessionRequests = eventStatusId == (int)EventStatusEnum.Published
            ? dto.Sessions
            : [];
        var firstSession = publicSessionRequests.MinBy(s => s.StartTime);
        var lastSession = publicSessionRequests.MaxBy(s => s.StartTime);
        var firstSessionLocal = firstSession is null
            ? (DateOnly?)null
            : _scheduleProjectionCalculator.Project(firstSession.StartTime, firstSession.EndTime, timezoneId).LocalStartDate;
        var lastSessionLocal = lastSession is null
            ? (DateOnly?)null
            : _scheduleProjectionCalculator.Project(lastSession.StartTime, lastSession.EndTime, timezoneId).LocalStartDate;

        return new Event
        {
            Title = dto.Title,
            Subtitle = dto.Subtitle,
            Description = dto.Description,
            Content = dto.Content,
            Slug = string.IsNullOrWhiteSpace(dto.Slug) ? SlugGenerator.FromTitle(dto.Title, "event") : dto.Slug,
            PublicCode = GeneratePublicCode(),
            EventTypeId = dto.EventTypeId,
            AudienceGenderId = dto.AudienceGenderId,
            AudienceAgeId = dto.AudienceAgeId,
            Price = dto.Price,
            CurrencyCode = dto.CurrencyCode,
            FeaturedImageId = dto.FeaturedImageId,
            IsRegistrationRequired = dto.IsRegistrationRequired,
            ExternalRegistrationUrl = dto.ExternalRegistrationUrl,
            EventStatusId = eventStatusId,
            VisibilityTypeId = dto.VisibilityTypeId == 0 ? 1 : dto.VisibilityTypeId,
            EventFormatId = dto.EventFormatId == 0 ? 1 : dto.EventFormatId,
            MadhabId = dto.MadhabId ?? dto.IslamicAspect?.MadhabId,
            Timezone = timezoneId,
            EventTimeZoneId = timezoneId,
            BackgroundColor = dto.BackgroundColor,
            BackgroundEffect = dto.BackgroundEffect,
            BackgroundImageId = dto.BackgroundImageId,
            EventSeriesId = dto.EventSeriesId,
            SeriesOrder = dto.SeriesOrder,
            RegistrationPolicyId = dto.RegistrationPolicyId,
            FirstSessionDate = firstSessionLocal,
            LastSessionDate = lastSessionLocal,
            FirstSessionStartUtc = firstSession?.StartTime.ToUniversalTime(),
            LastSessionStartUtc = lastSession?.StartTime.ToUniversalTime(),
            SessionCount = publicSessionRequests.Count,
            ActorId = actorResult.ActorId,
            Actor = null!,
            EventProvenanceTypeId = actorResult.IsCommunitySubmission
                ? (int)EventProvenanceTypeEnum.CommunityReported
                : (int)EventProvenanceTypeEnum.OrganizerCreated,
            SubmittedByUserId = actorResult.IsCommunitySubmission ? currentUserId : null,
            OrganizerActorId = actorResult.IsCommunitySubmission ? null : actorResult.ActorId,
            TenantId = _tenantContext.TenantId,
            Tenant = null!,
            TotalViews = 0,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!,
            CreatedAt = createdAt.UtcDateTime,
            CreatedBy = currentUserId,
            IsDeleted = false
        };
    }

    private static string GeneratePublicCode() => Guid.CreateVersion7().ToString("N")[..12];

    private async Task AssignFeaturedImageActorAsync(CreateEventRequest dto, Guid actorId)
    {
        if (!dto.FeaturedImageId.HasValue) return;

        var storageObject = await _storageObjectRepository.GetById(dto.FeaturedImageId.Value);
        if (storageObject is null) return;

        storageObject.ActorId = actorId;
        await _storageObjectRepository.Update(storageObject);
    }

    private async Task CreateEventIslamicAspectAsync(CreateEventRequest dto, Event eventEntity, CancellationToken ct)
    {
        if (dto.IslamicAspect is null) return;

        var aspect = new EventIslamicAspect
        {
            Id = eventEntity.Id,
            Event = null,
            MadhabId = dto.IslamicAspect.MadhabId,
            ReferencePrayer = dto.IslamicAspect.ReferencePrayer,
            PrayerTimeOffset = dto.IslamicAspect.PrayerTimeOffset,
            GenderMode = dto.IslamicAspect.GenderMode,
            IncludesQuranRecitation = dto.IslamicAspect.IncludesQuranRecitation,
            PrimaryLanguageId = dto.IslamicAspect.PrimaryLanguageId
        };

        await _eventIslamicAspectRepository.Upsert(aspect);
    }

    private async Task AssignInitialEventOwnerAsync(
        Explore.Domain.Event eventEntity,
        Guid creatorUserId,
        CancellationToken ct)
    {
        var assignment = EventRoleAssignment.Create(
            eventEntity.TenantId,
            eventEntity.Id,
            creatorUserId,
            (int)RoleEnum.EventOwner,
            EventRoleAssignmentStatus.Active,
            DateTime.UtcNow,
            expiresAtUtc: null,
            createdByUserId: creatorUserId);

        await _eventRoleAssignmentRepository.Create(assignment);
    }

    private async Task<(Dictionary<string, EventDay> ByKey, Dictionary<DateOnly, EventDay> ByDate)> CreateEventDaysAsync(
        CreateEventRequest dto,
        Event eventEntity,
        string timezoneId,
        CancellationToken ct)
    {
        var explicitDayByDate = dto.Days
            .GroupBy(d => d.LocalDate)
            .ToDictionary(g => g.Key, g => g.First());

        var sessionDates = dto.Sessions
            .Select(s => _scheduleProjectionCalculator.Project(s.StartTime, s.EndTime, timezoneId).LocalStartDate)
            .Distinct()
            .Order()
            .ToList();

        var shouldCreateSessionDays = sessionDates.Count > 1;
        var datesToCreate = explicitDayByDate.Keys
            .Concat(shouldCreateSessionDays ? sessionDates : Enumerable.Empty<DateOnly>())
            .Distinct()
            .Order()
            .ToList();

        var byKey = new Dictionary<string, EventDay>(StringComparer.OrdinalIgnoreCase);
        var byDate = new Dictionary<DateOnly, EventDay>();
        var sortOrder = 0;

        foreach (var localDate in datesToCreate)
        {
            explicitDayByDate.TryGetValue(localDate, out var requestDay);
            var day = new EventDay
            {
                EventId = eventEntity.Id,
                Event = null!,
                TenantId = _tenantContext.TenantId,
                Tenant = null!,
                LocalDate = localDate,
                Label = requestDay?.Label,
                Description = requestDay?.Description,
                BannerText = requestDay?.BannerText,
                BannerImageId = requestDay?.BannerImageId,
                IsPublished = requestDay?.IsPublished ?? true,
                SortOrder = requestDay?.SortOrder ?? sortOrder,
                AllowsDayScopeRegistration = requestDay?.AllowsDayScopeRegistration ?? false
            };

            day = await _eventDayRepository.Create(day);
            byDate[localDate] = day;

            if (!string.IsNullOrWhiteSpace(requestDay?.TempKey))
            {
                byKey[requestDay.TempKey.Trim()] = day;
            }

            sortOrder++;
        }

        return (byKey, byDate);
    }

    private async Task<Dictionary<string, Location>> CreateLocationsAsync(CreateEventRequest dto, CancellationToken ct)
    {
        var byKey = new Dictionary<string, Location>(StringComparer.OrdinalIgnoreCase);

        foreach (var locationDto in dto.Locations)
        {
            var location = new Location
            {
                FullName = locationDto.FullName,
                Country = locationDto.Country,
                City = locationDto.City,
                Timezone = locationDto.Timezone,
                TenantId = _tenantContext.TenantId,
                Tenant = null!,
                Pii = new LocationPii
                {
                    Address = locationDto.Address,
                    Postcode = locationDto.Postcode,
                    Latitude = locationDto.Latitude,
                    Longitude = locationDto.Longitude
                }
            };

            location = await _locationRepository.Create(location);
            byKey[locationDto.TempKey.Trim()] = location;
        }

        return byKey;
    }

    private async Task<Dictionary<string, LocationRoom>> CreateRoomsAsync(
        CreateEventRequest dto,
        IReadOnlyDictionary<string, Location> locationMap,
        CancellationToken ct)
    {
        var byKey = new Dictionary<string, LocationRoom>(StringComparer.OrdinalIgnoreCase);

        foreach (var roomDto in dto.Rooms)
        {
            var locationId = ResolveLocationId(roomDto.LocationTempKey, roomDto.LocationId, locationMap)
                ?? throw new InvalidOperationException("Room location was not resolved.");

            var room = new LocationRoom
            {
                LocationId = locationId,
                Location = null!,
                TenantId = _tenantContext.TenantId,
                Tenant = null!,
                Name = roomDto.Name,
                Slug = string.IsNullOrWhiteSpace(roomDto.Slug) ? SlugGenerator.FromTitle(roomDto.Name, "room") : roomDto.Slug,
                Description = roomDto.Description,
                Capacity = roomDto.Capacity,
                SortOrder = roomDto.SortOrder
            };

            room = await _locationRoomRepository.Create(room);
            byKey[roomDto.TempKey.Trim()] = room;
        }

        return byKey;
    }

    private async Task CreateSessionsAsync(
        CreateEventRequest dto,
        Event eventEntity,
        IReadOnlyDictionary<string, Location> locationMap,
        (Dictionary<string, EventDay> ByKey, Dictionary<DateOnly, EventDay> ByDate) dayMaps,
        IReadOnlyDictionary<string, LocationRoom> roomMap,
        string timezoneId,
        Guid currentUserId,
        DateTimeOffset createdAt,
        CancellationToken ct)
    {
        if (dto.Sessions.Count == 0)
        {
            if (eventEntity.EventStatusId != (int)EventStatusEnum.Draft)
            {
                await CreateDefaultSessionAsync(dto, eventEntity, locationMap, roomMap, timezoneId, ct);
            }
            return;
        }

        var index = 0;
        foreach (var sessionDto in dto.Sessions.OrderBy(s => s.StartTime).ThenBy(s => s.SortOrder))
        {
            index++;
            var session = new EventSession
            {
                EventId = eventEntity.Id,
                Event = null!,
                TenantId = _tenantContext.TenantId,
                Tenant = null!,
                Title = string.IsNullOrWhiteSpace(sessionDto.Title) ? eventEntity.Title : sessionDto.Title,
                Description = sessionDto.Description,
                LocationId = ResolveLocationId(sessionDto.LocationTempKey, sessionDto.LocationId, locationMap, sessionDto.RoomTempKey, roomMap),
                RoomId = ResolveRoomId(sessionDto.RoomTempKey, sessionDto.RoomId, roomMap),
                FeaturedImageId = sessionDto.FeaturedImageId,
                SortOrder = sessionDto.SortOrder == 0 ? index - 1 : sessionDto.SortOrder,
                MaxAudienceAttendees = sessionDto.MaxAudienceAttendees,
                CurrentAudienceAttendees = 0,
                EventSessionKindId = sessionDto.EventSessionKindId,
                EventSessionStatusId = eventEntity.EventStatusId == (int)EventStatusEnum.Published
                    ? (int)EventSessionStatusEnum.Published
                    : (int)EventSessionStatusEnum.Draft,
                RegistrationModeId = sessionDto.RegistrationModeId ?? (dto.IsRegistrationRequired ? 1 : null),
                Price = sessionDto.Price,
                CurrencyCode = sessionDto.CurrencyCode,
                Slug = string.IsNullOrWhiteSpace(sessionDto.Slug)
                    ? SlugGenerator.FromTitle(sessionDto.Title ?? $"{eventEntity.Title}-session-{index}", "session")
                    : sessionDto.Slug
            };

            EventLocation eventLocation = await _eventLocationAttachmentService.ResolveAsync(
                eventEntity.Id,
                session.LocationId,
                currentEventLocationId: null,
                ct);
            session.AssignEventLocation(eventLocation);

            session.Reschedule(sessionDto.StartTime, sessionDto.EndTime, timezoneId, _scheduleProjectionCalculator);
            session.EventDayId = session.LocalStartDate is not null
                ? ResolveDayId(sessionDto.DayTempKey, session.LocalStartDate.Value, dayMaps)
                : null;
            session = await PersistSessionWithRoomGuardAsync(session, ct);

            await CreateSessionAspectsAsync(sessionDto, session, ct);
            await CreateSessionLanguagesAsync(sessionDto, session, ct);
            await CreateSessionSpeakersAsync(sessionDto, session, ct);
            await InstantiateSessionTemplatePropertiesAsync(sessionDto, session, currentUserId, createdAt, ct);
        }
    }

    private async Task CreateDefaultSessionAsync(
        CreateEventRequest dto,
        Event eventEntity,
        IReadOnlyDictionary<string, Location> locationMap,
        IReadOnlyDictionary<string, LocationRoom> roomMap,
        string timezoneId,
        CancellationToken ct)
    {
        var roomId = ResolveDefaultRoomId(dto, roomMap);
        var locationId = ResolveDefaultLocationId(dto, locationMap, roomMap, roomId);
        var session = new EventSession
        {
            EventId = eventEntity.Id,
            Event = null!,
            TenantId = _tenantContext.TenantId,
            Tenant = null!,
            Title = eventEntity.Title,
            LocationId = locationId,
            RoomId = roomId,
            SortOrder = 0,
            CurrentAudienceAttendees = 0,
            EventSessionStatusId = eventEntity.EventStatusId == (int)EventStatusEnum.Published
                ? (int)EventSessionStatusEnum.Published
                : (int)EventSessionStatusEnum.Draft,
            RegistrationModeId = dto.IsRegistrationRequired ? 1 : null,
            Slug = SlugGenerator.FromTitle($"{eventEntity.Title}-session-1", "session")
        };

        EventLocation eventLocation = await _eventLocationAttachmentService.ResolveAsync(
            eventEntity.Id,
            session.LocationId,
            currentEventLocationId: null,
            ct);
        session.AssignEventLocation(eventLocation);

        session.ReprojectLocalTimes(timezoneId, _scheduleProjectionCalculator);
        await PersistSessionWithRoomGuardAsync(session, ct);
    }

    private async Task<EventSession> PersistSessionWithRoomGuardAsync(EventSession session, CancellationToken ct)
    {
        if (session.RoomId is not null && session.StartTime is not null && session.EndTime is not null)
        {
            var conflicts = await _eventSessionRepository.GetOverlappingSessionsInRoomAsync(
                session.RoomId.Value,
                session.StartTime.Value,
                session.EndTime.Value,
                excludeSessionId: null,
                ct);

            if (conflicts.Count > 0)
            {
                throw new RoomScheduleConflictException(session.RoomId.Value, conflicts.Select(s => s.Id).ToList());
            }
        }

        return await _eventSessionRepository.Create(session);
    }

    private async Task CreateSessionAspectsAsync(CreateEventSessionRequest sessionDto, EventSession session, CancellationToken ct)
    {
        if (sessionDto.IslamicAspect is null) return;

        var aspect = new EventSessionIslamicAspect
        {
            EventSessionId = session.Id,
            EventSession = null,
            RequiresWudu = sessionDto.IslamicAspect.RequiresWudu,
            RitualRequirementsJson = sessionDto.IslamicAspect.RitualRequirementsJson
        };
        aspect.ApplyScheduling(
            sessionDto.IslamicAspect.StartTimeType,
            sessionDto.IslamicAspect.ReferencePrayer,
            sessionDto.IslamicAspect.OffsetMinutes);
        aspect.ApplyEndTimeScheduling(
            sessionDto.EndTimeType,
            sessionDto.IslamicAspect.EndReferencePrayer,
            sessionDto.IslamicAspect.EndOffsetMinutes);

        await _eventSessionIslamicAspectRepository.Create(aspect);
    }

    private async Task CreateSessionLanguagesAsync(CreateEventSessionRequest sessionDto, EventSession session, CancellationToken ct)
    {
        foreach (var languageId in sessionDto.LanguageIds.Distinct())
        {
            await _eventSessionLanguageRepository.Create(new EventSessionLanguage
            {
                EventSessionId = session.Id,
                EventSession = null!,
                LanguageId = languageId,
                Language = null!,
                TenantId = _tenantContext.TenantId,
                Tenant = null!
            });
        }
    }

    private async Task CreateSessionSpeakersAsync(CreateEventSessionRequest sessionDto, EventSession session, CancellationToken ct)
    {
        foreach (var actorId in sessionDto.SpeakerActorIds.Distinct())
        {
            await _eventSessionSpeakerRepository.Create(new EventSessionSpeaker
            {
                EventSessionId = session.Id,
                EventSession = null!,
                ActorId = actorId,
                Actor = null!,
                TenantId = _tenantContext.TenantId,
                Tenant = null!
            });
        }
    }

    private async Task CreateEventAgendaItemsAsync(
        CreateEventRequest dto,
        Event eventEntity,
        IReadOnlyDictionary<string, Location> locationMap,
        (Dictionary<string, EventDay> ByKey, Dictionary<DateOnly, EventDay> ByDate) dayMaps,
        IReadOnlyDictionary<string, LocationRoom> roomMap,
        string timezoneId,
        CancellationToken ct)
    {
        foreach (var itemDto in dto.AgendaItems.OrderBy(i => i.StartTime).ThenBy(i => i.SortOrder))
        {
            var agendaItem = new EventAgendaItem
            {
                EventId = eventEntity.Id,
                Event = null!,
                TenantId = _tenantContext.TenantId,
                Tenant = null!,
                Title = itemDto.Title,
                Description = itemDto.Description,
                LocationId = ResolveLocationId(itemDto.LocationTempKey, itemDto.LocationId, locationMap, itemDto.RoomTempKey, roomMap),
                RoomId = ResolveRoomId(itemDto.RoomTempKey, itemDto.RoomId, roomMap),
                KindId = itemDto.KindId,
                SortOrder = itemDto.SortOrder
            };

            EventLocation eventLocation = await _eventLocationAttachmentService.ResolveAsync(
                eventEntity.Id,
                agendaItem.LocationId,
                currentEventLocationId: null,
                ct);
            agendaItem.AssignEventLocation(eventLocation);

            agendaItem.Reschedule(itemDto.StartTime, itemDto.EndTime, timezoneId, _scheduleProjectionCalculator);
            agendaItem.EventDayId = ResolveDayId(itemDto.DayTempKey, agendaItem.LocalStartDate, dayMaps);
            await _eventAgendaItemRepository.Create(agendaItem);
        }
    }

    private async Task CreateCategoryAndTagAssignmentsAsync(CreateEventRequest dto, Event eventEntity, CancellationToken ct)
    {
        foreach (var categoryId in dto.CategoryIds.Distinct())
        {
            await _eventCategoriesRepository.Create(new Explore.Domain.EventCategories
            {
                EventId = eventEntity.Id,
                Event = null!,
                CategoryId = categoryId,
                Category = null!,
                TenantId = _tenantContext.TenantId,
                Tenant = null!
            });
        }

        foreach (var tagId in dto.TagIds.Distinct())
        {
            await _eventTagsRepository.Create(new Explore.Domain.EventTags
            {
                EventId = eventEntity.Id,
                Event = null!,
                TagId = tagId,
                Tag = null!,
                TenantId = _tenantContext.TenantId,
                Tenant = null!
            });
        }
    }

    private async Task InstantiateTemplatePropertiesAsync(CreateEventRequest dto, Event eventEntity, Guid currentUserId, DateTimeOffset createdAt, CancellationToken ct)
    {
        if (!dto.TemplateId.HasValue) return;

        var template = await _eventTemplateRepository.GetTemplateWithDetails(dto.TemplateId.Value);
        if (template is not { IsPublished: true, IsActive: true }) return;

        eventEntity.SourceTemplateId = template.Id;
        eventEntity.SourceTemplateKey = template.TemplateKey;
        eventEntity.SourceTemplateVersion = template.Version;
        eventEntity.InstantiatedFromTemplateAt = createdAt;
        eventEntity.LastSyncedFromTemplateAt = createdAt;
        await _eventRepository.Update(eventEntity);

        var instantiationResult = _eventTemplateInstantiationService.InstantiateFromTemplate(
            eventEntity.Id, _tenantContext.TenantId, template, currentUserId.ToString());

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

        await _eventCustomPropertyProjectionUpdater.RefreshForEventAsync(eventEntity.Id, ct);
    }

    private async Task InstantiateSessionTemplatePropertiesAsync(
        CreateEventSessionRequest dto,
        EventSession session,
        Guid currentUserId,
        DateTimeOffset createdAt,
        CancellationToken ct)
    {
        if (!dto.SessionTemplateId.HasValue) return;

        var template = await _eventSessionTemplateRepository.GetSessionTemplateWithDetails(dto.SessionTemplateId.Value);
        if (template is not { IsPublished: true, IsActive: true }) return;

        session.SourceTemplateId = template.Id;
        session.SourceTemplateKey = template.SessionTemplateKey;
        session.SourceTemplateVersion = template.Version;
        session.InstantiatedFromTemplateAt = createdAt;
        session.LastSyncedFromTemplateAt = createdAt;
        await _eventSessionRepository.Update(session);

        var instantiationResult = _eventSessionTemplateInstantiationService.InstantiateFromSessionTemplate(
            session.Id,
            _tenantContext.TenantId,
            template,
            currentUserId.ToString());

        foreach (var runtimeDef in instantiationResult.Definitions)
        {
            runtimeDef.Definition.DefaultOptionId = null;
            await _eventSessionCustomPropertyRepository.CreateWithOptions(
                runtimeDef.Definition,
                runtimeDef.Options,
                runtimeDef.DefaultOptionId,
                ct);

            if (runtimeDef.DefaultValue != null)
            {
                await _eventSessionCustomPropertyRepository.SetValue(runtimeDef.DefaultValue, ct);
            }
        }

        await _eventSessionCustomPropertyProjectionUpdater.RefreshForEventSessionAsync(session.Id, ct);
    }

    private static Guid? ResolveRoomId(string? roomTempKey, Guid? existingRoomId, IReadOnlyDictionary<string, LocationRoom> roomMap)
    {
        if (!string.IsNullOrWhiteSpace(roomTempKey) && roomMap.TryGetValue(roomTempKey.Trim(), out var room))
        {
            return room.Id;
        }

        return existingRoomId;
    }

    private static Guid? ResolveDefaultRoomId(
        CreateEventRequest dto,
        IReadOnlyDictionary<string, LocationRoom> roomMap)
    {
        var roomRequest = dto.Rooms
            .OrderBy(room => room.SortOrder)
            .FirstOrDefault();

        return roomRequest is null
            ? null
            : ResolveRoomId(roomRequest.TempKey, null, roomMap);
    }

    private static Guid? ResolveDefaultLocationId(
        CreateEventRequest dto,
        IReadOnlyDictionary<string, Location> locationMap,
        IReadOnlyDictionary<string, LocationRoom> roomMap,
        Guid? roomId)
    {
        if (roomId.HasValue)
        {
            var room = roomMap.Values.FirstOrDefault(candidate => candidate.Id == roomId.Value);
            if (room is not null)
            {
                return room.LocationId;
            }
        }

        var locationRequest = dto.Locations.FirstOrDefault();
        if (locationRequest is not null
            && locationMap.TryGetValue(locationRequest.TempKey.Trim(), out var location))
        {
            return location.Id;
        }

        return null;
    }

    private static Guid? ResolveLocationId(
        string? locationTempKey,
        Guid? existingLocationId,
        IReadOnlyDictionary<string, Location> locationMap,
        string? roomTempKey = null,
        IReadOnlyDictionary<string, LocationRoom>? roomMap = null)
    {
        if (!string.IsNullOrWhiteSpace(locationTempKey) && locationMap.TryGetValue(locationTempKey.Trim(), out var location))
        {
            return location.Id;
        }

        if (existingLocationId.HasValue)
        {
            return existingLocationId;
        }

        if (!string.IsNullOrWhiteSpace(roomTempKey) && roomMap?.TryGetValue(roomTempKey.Trim(), out var room) == true)
        {
            return room.LocationId;
        }

        return null;
    }

    private static Guid? ResolveDayId(
        string? dayTempKey,
        DateOnly localDate,
        (Dictionary<string, EventDay> ByKey, Dictionary<DateOnly, EventDay> ByDate) dayMaps)
    {
        if (!string.IsNullOrWhiteSpace(dayTempKey) && dayMaps.ByKey.TryGetValue(dayTempKey.Trim(), out var keyedDay))
        {
            return keyedDay.Id;
        }

        return dayMaps.ByDate.TryGetValue(localDate, out var dateDay) ? dateDay.Id : null;
    }

    private static string ResolveTimezoneId(CreateEventRequest dto) =>
        ScheduleTimeZoneResolver.NormalizeOrUtc(
            !string.IsNullOrWhiteSpace(dto.EventTimeZoneId)
                ? dto.EventTimeZoneId
                : dto.Timezone);

}

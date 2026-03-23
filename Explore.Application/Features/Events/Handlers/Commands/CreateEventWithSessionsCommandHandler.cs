// ABOUTME: Handler for creating an event together with its initial sessions in one operation.
// ABOUTME: Orchestrates event + session creation atomically.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event.Validators;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.Events.Handlers.Commands;

/// <summary>
/// Handler for creating an event along with its sessions in a single transaction.
/// FirstSessionDate and LastSessionDate are computed from the provided sessions.
/// </summary>
public class CreateEventWithSessionsCommandHandler : IRequestHandler<CreateEventWithSessionsCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IEventSessionIslamicAspectRepository _eventSessionIslamicAspectRepository;
    private readonly IEventSessionLanguageRepository _eventSessionLanguageRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrganizationMemberRepository _organizationMemberRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly IHierarchicalSettingsResolver _settingsResolver;
    private readonly IAudienceAgeRepository _audienceAgeRepository;
    private readonly IAudienceGenderRepository _audienceGenderRepository;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
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
        IActorRepository actorRepository,
        IOrganizationRepository organizationRepository,
        IOrganizationMemberRepository organizationMemberRepository,
        IGroupRepository groupRepository,
        IGroupMemberRepository groupMemberRepository,
        IHierarchicalSettingsResolver settingsResolver,
        IAudienceAgeRepository audienceAgeRepository,
        IAudienceGenderRepository audienceGenderRepository,
        IEventTypeRepository eventTypeRepository,
        IStorageObjectRepository storageObjectRepository,
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
        _actorRepository = actorRepository;
        _organizationRepository = organizationRepository;
        _organizationMemberRepository = organizationMemberRepository;
        _groupRepository = groupRepository;
        _groupMemberRepository = groupMemberRepository;
        _settingsResolver = settingsResolver;
        _audienceAgeRepository = audienceAgeRepository;
        _audienceGenderRepository = audienceGenderRepository;
        _eventTypeRepository = eventTypeRepository;
        _storageObjectRepository = storageObjectRepository;
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

        // Get the authenticated user's ID
        var currentUserId = _userContext.GetRequiredUserId();

        // ===== VALIDATION =====
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

        // ===== RESOLVE ACTOR ID =====
        Guid actorId;
        var dto = request.EventWithSessionsDto;
        var userSubmissionEnabled = await _settingsResolver.ResolveAsync<bool>(
            "events.user_submission_enabled", new SettingContext(TenantId: _tenantContext.TenantId), cancellationToken);
        var publishingPolicy = userSubmissionEnabled
            ? EventPublishingPolicyEnum.OrganizationGroupAndUserReported
            : EventPublishingPolicyEnum.OrganizationAndGroupOnly;

        if (dto.OrganizationId.HasValue)
        {
            // ORGANIZATION CONTEXT
            var organizationId = dto.OrganizationId.Value;

            // SECURITY: Verify user has event:create permission for this organization
            var hasPermission = await _organizationMemberRepository.HasPermissionInOrganization(organizationId, currentUserId, PermissionCodes.EventCreate);
            if (!hasPermission)
            {
                response.Success = false;
                response.Message = "You do not have permission to create events for this organization.";
                response.Errors = new List<string>
                {
                    "Your role in the organization does not include event creation permission."
                };
                return response;
            }

            // Get organization's actor
            var organizationActor = await _actorRepository.GetActorByOrganizationId(organizationId);
            if (organizationActor == null)
            {
                response.Success = false;
                response.Message = "Organization does not have an associated actor.";
                response.Errors = new List<string>
                {
                    "The organization is not properly configured. Please contact support."
                };
                return response;
            }

            actorId = organizationActor.Id;
        }
        else if (dto.GroupId.HasValue)
        {
            var groupId = dto.GroupId.Value;
            var hasPermission = await _groupMemberRepository.HasPermissionInGroup(groupId, currentUserId, PermissionCodes.EventCreate);
            if (!hasPermission)
            {
                response.Success = false;
                response.Message = "You do not have permission to create events for this group.";
                response.Errors = new List<string>
                {
                    "Your role in the group does not include event creation permission."
                };
                return response;
            }

            var groupActor = await _actorRepository.GetActorByGroupId(groupId);
            if (groupActor == null)
            {
                response.Success = false;
                response.Message = "Group does not have an associated actor.";
                response.Errors = new List<string>
                {
                    "The group is not properly configured. Please contact support."
                };
                return response;
            }

            actorId = groupActor.Id;
        }
        else
        {
            // PERSONAL CONTEXT
            if (publishingPolicy == EventPublishingPolicyEnum.OrganizationAndGroupOnly)
            {
                response.Success = false;
                response.Message = "Personal event publishing is disabled for this tenant.";
                response.Errors = new List<string>
                {
                    "Select an organization or group to publish this event."
                };
                return response;
            }

            var userActor = await _actorRepository.GetActorByUserId(currentUserId);
            if (userActor == null)
            {
                response.Success = false;
                response.Message = "Your personal actor was not found.";
                response.Errors = new List<string>
                {
                    "Your account is not properly set up. Please sync your profile first."
                };
                return response;
            }

            actorId = userActor.Id;
        }

        // ===== COMPUTE FIRST/LAST SESSION DATES FROM SESSIONS =====
        var sessions = dto.Sessions;
        var firstSessionDate = sessions.Min(s => s.StartTime);
        var lastSessionDate = sessions.Max(s => s.EndTime);

        // Convert to DateOnly for storage in Event entity
        var firstSessionDateOnly = DateOnly.FromDateTime(firstSessionDate.UtcDateTime);
        var lastSessionDateOnly = DateOnly.FromDateTime(lastSessionDate.UtcDateTime);

        // Build the event entity BEFORE the lambda — for retry safety (no random values inside)
        var @event = new Event
        {
            Title = dto.Title,
            Description = dto.Description,
            Slug = string.IsNullOrWhiteSpace(dto.Slug) ? GenerateSlug(dto.Title) : dto.Slug,
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
            ActorId = actorId,
            Actor = null!,
            TenantId = _tenantContext.TenantId,
            Tenant = null!,
            TotalViews = 0,
            IsUserReported = !dto.OrganizationId.HasValue && !dto.GroupId.HasValue,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!,
            FirstSessionDate = firstSessionDateOnly,
            LastSessionDate = lastSessionDateOnly,
            SessionCount = sessions.Count
        };

        // Atomic writes: event + storage + all sessions + Islamic aspects + languages
        var eventId = await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            @event = await _eventRepository.Create(@event);
            Console.WriteLine($"[CREATE EVENT WITH SESSIONS] Event created with ID: {@event.Id}");

            if (dto.FeaturedImageId.HasValue)
            {
                var storageObject = await _storageObjectRepository.GetById(dto.FeaturedImageId.Value);
                if (storageObject != null)
                {
                    storageObject.ActorId = actorId;
                    await _storageObjectRepository.Update(storageObject);
                    Console.WriteLine($"[CREATE EVENT WITH SESSIONS] StorageObject {storageObject.Id} ActorId updated to {actorId}");
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
                    Slug = GenerateSlug(string.IsNullOrWhiteSpace(sessionDto.Title) ? $"{@event.Title}-session-{sessionIndex}" : sessionDto.Title)
                };

                eventSession = await _eventSessionRepository.Create(eventSession);
                Console.WriteLine($"[CREATE EVENT WITH SESSIONS] EventSession {sessionIndex} created with ID: {eventSession.Id}");

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

                if (sessionDto.LanguageIds.Any())
                {
                    Console.WriteLine($"[CREATE EVENT WITH SESSIONS] {sessionDto.LanguageIds.Count} languages assigned to session {eventSession.Id}");
                }
            }

            return @event.Id;
        }, cancellationToken);

        response.Success = true;
        response.Id = eventId;
        response.Message = $"Event and {sessions.Count} session(s) created successfully.";

        return response;
    }

    /// <summary>
    /// Generate a URL-friendly slug from the title.
    /// </summary>
    private static string GenerateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return $"event-{Guid.NewGuid().ToString("N").Substring(0, 8)}";

        var slug = title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace(".", "")
            .Replace(",", "");

        // Remove any non-alphanumeric characters except hyphens
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");

        // Limit length
        if (slug.Length > 50)
            slug = slug.Substring(0, 50);

        return slug;
    }
}

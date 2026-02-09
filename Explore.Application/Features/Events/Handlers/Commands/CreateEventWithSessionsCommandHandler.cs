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
using Explore.Domain;
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
    private readonly IEventSessionLanguageRepository _eventSessionLanguageRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrganizationMemberRepository _organizationMemberRepository;
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

    public CreateEventWithSessionsCommandHandler(
        IEventRepository eventRepository,
        IEventSessionRepository eventSessionRepository,
        IEventSessionLanguageRepository eventSessionLanguageRepository,
        IActorRepository actorRepository,
        IOrganizationRepository organizationRepository,
        IOrganizationMemberRepository organizationMemberRepository,
        IAudienceAgeRepository audienceAgeRepository,
        IAudienceGenderRepository audienceGenderRepository,
        IEventTypeRepository eventTypeRepository,
        IStorageObjectRepository storageObjectRepository,
        ILocationRepository locationRepository,
        IRegistrationModeRepository registrationModeRepository,
        ILanguageRepository languageRepository,
        IUserContext userContext,
        ITenantContext tenantContext,
        IMapper mapper)
    {
        _eventRepository = eventRepository;
        _eventSessionRepository = eventSessionRepository;
        _eventSessionLanguageRepository = eventSessionLanguageRepository;
        _actorRepository = actorRepository;
        _organizationRepository = organizationRepository;
        _organizationMemberRepository = organizationMemberRepository;
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

        if (dto.OrganizationId.HasValue)
        {
            // ORGANIZATION CONTEXT
            var organizationId = dto.OrganizationId.Value;

            // SECURITY: Verify user has admin permissions for this organization
            var isAdmin = await _organizationMemberRepository.IsUserAdminOfOrganization(organizationId, currentUserId);
            if (!isAdmin)
            {
                response.Success = false;
                response.Message = "You do not have permission to create events for this organization.";
                response.Errors = new List<string>
                {
                    "User must be a Creator, Co-Owner, or Admin of the organization to create events."
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
        else
        {
            // PERSONAL CONTEXT
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

        // ===== CREATE EVENT =====
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
            EventStatusId = dto.EventStatusId == 0 ? 1 : dto.EventStatusId, // Default: Draft
            VisibilityTypeId = dto.VisibilityTypeId == 0 ? 1 : dto.VisibilityTypeId, // Default: Public
            EventFormatId = dto.EventFormatId == 0 ? 1 : dto.EventFormatId, // Default: In-Person
            MadhabId = dto.MadhabId,
            Timezone = dto.Timezone,
            EventUrl = dto.EventUrl,
            ActorId = actorId,
            Actor = null!,
            TenantId = _tenantContext.TenantId,
            Tenant = null!,
            TotalViews = 0,
            IsUserReported = !dto.OrganizationId.HasValue,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!,
            // Computed from sessions
            FirstSessionDate = firstSessionDateOnly,
            LastSessionDate = lastSessionDateOnly,
            SessionCount = sessions.Count
        };

        @event = await _eventRepository.Create(@event);
        Console.WriteLine($"[CREATE EVENT WITH SESSIONS] Event created with ID: {@event.Id}");

        // ===== UPDATE STORAGE OBJECT OWNERSHIP =====
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

        // ===== CREATE EVENT SESSIONS =====
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
                Slug = GenerateSlug(string.IsNullOrWhiteSpace(sessionDto.Title) ? $"{@event.Title}-session-{sessionIndex}" : sessionDto.Title)
            };

            eventSession = await _eventSessionRepository.Create(eventSession);
            Console.WriteLine($"[CREATE EVENT WITH SESSIONS] EventSession {sessionIndex} created with ID: {eventSession.Id}");

            // Create session-language associations
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

        response.Success = true;
        response.Id = @event.Id;
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

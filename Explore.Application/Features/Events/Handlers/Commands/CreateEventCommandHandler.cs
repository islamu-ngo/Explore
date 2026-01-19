using System;
using System.Linq;
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
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.Events.Handlers.Commands
{
    public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, BaseCommandResponse<Guid>>
    {
        private readonly IEventRepository _eventRepository;
        private readonly IEventSessionRepository _eventSessionRepository;
        private readonly IActorRepository _actorRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IOrganizationMemberRepository _organizationMemberRepository;
        private readonly IAudienceAgeRepository _audienceAgeRepository;
        private readonly IAudienceGenderRepository _audienceGenderRepository;
        private readonly IEventTypeRepository _eventTypeRepository;
        private readonly IStorageObjectRepository _storageObjectRepository;
        private readonly IUserContext _userContext;
        private readonly ITenantContext _tenantContext;
        private readonly IMapper _mapper;

        public CreateEventCommandHandler(
            IEventRepository eventRepository,
            IEventSessionRepository eventSessionRepository,
            IActorRepository actorRepository,
            IOrganizationRepository organizationRepository,
            IOrganizationMemberRepository organizationMemberRepository,
            IAudienceAgeRepository audienceAgeRepository,
            IAudienceGenderRepository audienceGenderRepository,
            IEventTypeRepository eventTypeRepository,
            IStorageObjectRepository storageObjectRepository,
            IUserContext userContext,
            ITenantContext tenantContext,
            IMapper mapper)
        {
            _eventRepository = eventRepository;
            _eventSessionRepository = eventSessionRepository;
            _actorRepository = actorRepository;
            _organizationRepository = organizationRepository;
            _organizationMemberRepository = organizationMemberRepository;
            _audienceAgeRepository = audienceAgeRepository;
            _audienceGenderRepository = audienceGenderRepository;
            _eventTypeRepository = eventTypeRepository;
            _storageObjectRepository = storageObjectRepository;
            _userContext = userContext;
            _tenantContext = tenantContext;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            // Get the authenticated user's Keycloak ID
            var currentUserId = _userContext.GetRequiredUserId();

            // Validate the DTO
            var validator = new CreateEventDtoValidator(
                _audienceAgeRepository,
                _audienceGenderRepository,
                _eventTypeRepository,
                _organizationRepository,
                _storageObjectRepository);

            var validationResult = await validator.ValidateAsync(request.EventDto, cancellationToken);
            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Event creation failed due to validation errors.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            // Resolve the ActorId based on context
            Guid actorId;

            if (request.EventDto.OrganizationId.HasValue)
            {
                // ===== ORGANIZATION CONTEXT =====
                // User wants to create event for an organization
                var organizationId = request.EventDto.OrganizationId.Value;

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

                // Find Actor where OrganizationId == request.OrganizationId
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
                // ===== IDENTITY CONTEXT (Personal) =====
                // Business rule: Events should only be created by organizations
                // But we keep the personal actor fallback for backward compatibility
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

            // Map DTO to entity
            var @event = _mapper.Map<Event>(request.EventDto);

            // Set the resolved ActorId and initialize defaults
            @event.ActorId = actorId;
            @event.TotalViews = 0;
            @event.TenantId = _tenantContext.TenantId;

            // Set defaults for status and visibility if not provided
            if (@event.EventStatusId == 0) @event.EventStatusId = 1; // Draft
            if (@event.VisibilityTypeId == 0) @event.VisibilityTypeId = 1; // Public
            if (@event.EventFormatId == 0) @event.EventFormatId = 1; // In-Person

            // Persist the event
            @event = await _eventRepository.Create(@event);
            Console.WriteLine($"[CREATE EVENT] Event created with ID: {@event.Id}");

            // ===== UPDATE STORAGE OBJECT OWNERSHIP =====
            // If a featured image was uploaded, update its ActorId to link it to the event's actor
            if (request.EventDto.FeaturedImageId.HasValue)
            {
                var storageObject = await _storageObjectRepository.GetById(request.EventDto.FeaturedImageId.Value);
                if (storageObject != null)
                {
                    storageObject.ActorId = actorId;
                    await _storageObjectRepository.Update(storageObject);
                    Console.WriteLine($"[CREATE EVENT] StorageObject {storageObject.Id} ActorId updated to {actorId}");
                }
            }

            // ===== CREATE DEFAULT EVENT SESSION =====
            // Each event must have at least one session
            // Use the dates from the DTO to create the first session
            var eventSession = new EventSession
            {
                EventId = @event.Id,
                TenantId = _tenantContext.TenantId,
                Title = @event.Title, // Use event title as default session title
                Description = @event.Description,
                StartTime = request.EventDto.FirstSessionDate ?? DateTimeOffset.UtcNow,
                EndTime = request.EventDto.LastSessionDate ?? DateTimeOffset.UtcNow.AddHours(2),
                LocationId = null, // Location can be added later
                MaxAudienceAttendees = null,
                CurrentAudienceAttendees = 0,
                RegistrationModeId = request.EventDto.IsRegistrationRequired ? 1 : null, // 1 = Required
                Slug = GenerateSlug(@event.Title)
            };

            await _eventSessionRepository.Create(eventSession);
            Console.WriteLine($"[CREATE EVENT] Default EventSession created with ID: {eventSession.Id}");

            response.Success = true;
            response.Id = @event.Id;
            response.Message = "Event and session created successfully.";

            return response;
        }

        /// <summary>
        /// Generate a URL-friendly slug from the title
        /// </summary>
        private string GenerateSlug(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return $"session-{Guid.NewGuid().ToString("N").Substring(0, 8)}";

            var slug = title.ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("'", "")
                .Replace("\"", "")
                .Replace(".", "")
                .Replace(",", "");

            // Remove any non-alphanumeric characters except hyphens
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");

            // Limit length
            if (slug.Length > 50)
                slug = slug.Substring(0, 50);

            return slug;
        }
    }
}

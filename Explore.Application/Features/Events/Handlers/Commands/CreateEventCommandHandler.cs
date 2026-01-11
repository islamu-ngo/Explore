using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Identity;
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
        private readonly IActorRepository _actorRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IOrganizationMemberRepository _organizationMemberRepository;
        private readonly IAudienceAgeRepository _audienceAgeRepository;
        private readonly IAudienceGenderRepository _audienceGenderRepository;
        private readonly IEventTypeRepository _eventTypeRepository;
        private readonly IStorageObjectRepository _storageObjectRepository;
        private readonly IUserContext _userContext;
        private readonly IMapper _mapper;

        public CreateEventCommandHandler(
            IEventRepository eventRepository,
            IActorRepository actorRepository,
            IOrganizationRepository organizationRepository,
            IOrganizationMemberRepository organizationMemberRepository,
            IAudienceAgeRepository audienceAgeRepository,
            IAudienceGenderRepository audienceGenderRepository,
            IEventTypeRepository eventTypeRepository,
            IStorageObjectRepository storageObjectRepository,
            IUserContext userContext,
            IMapper mapper)
        {
            _eventRepository = eventRepository;
            _actorRepository = actorRepository;
            _organizationRepository = organizationRepository;
            _organizationMemberRepository = organizationMemberRepository;
            _audienceAgeRepository = audienceAgeRepository;
            _audienceGenderRepository = audienceGenderRepository;
            _eventTypeRepository = eventTypeRepository;
            _storageObjectRepository = storageObjectRepository;
            _userContext = userContext;
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
                // User wants to create event under their personal actor
                // Find Actor where UserId == currentUserId
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

            // Persist the event in a single atomic operation
            @event = await _eventRepository.Create(@event);

            response.Success = true;
            response.Id = @event.Id;
            response.Message = "Event created successfully.";

            return response;
        }
    }
}
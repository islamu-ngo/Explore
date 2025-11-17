using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
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
        private readonly IAudienceAgeRepository _audienceAgeRepository;
        private readonly IAudienceGenderRepository _audienceGenderRepository;
        private readonly IEventTypeRepository _eventTypeRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IStorageObjectRepository _storageObjectRepository;

        private readonly IMapper _mapper;

        public CreateEventCommandHandler(
            IEventRepository eventRepository, 
            IAudienceAgeRepository audienceAgeRepository,
            IAudienceGenderRepository audienceGenderRepository,
            IEventTypeRepository eventTypeRepository,
            IOrganizationRepository organizationRepository,
            IStorageObjectRepository storageObjectRepository, 
            IMapper mapper)
        {
            _eventRepository = eventRepository;
            _audienceAgeRepository = audienceAgeRepository;
            _audienceGenderRepository = audienceGenderRepository;
            _eventTypeRepository = eventTypeRepository;
            _organizationRepository = organizationRepository;
            _storageObjectRepository = storageObjectRepository;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            var validator = new CreateEventDtoValidator(_audienceAgeRepository, _audienceGenderRepository, _eventTypeRepository, _organizationRepository, _storageObjectRepository);
            var validationResult = await validator.ValidateAsync(request.EventDto);
            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Event creation failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            var @event = _mapper.Map<Event>(request.EventDto);
            @event.ProgramTypeId = (int)ProgramTypeEnum.Event;
            @event.TotalViews = 0;
            @event = await _eventRepository.Create(@event);

            response.Success = true;
            response.Id = @event.Id;
            response.Message = "Event created successfully.";

            return response;
        }
    }
}
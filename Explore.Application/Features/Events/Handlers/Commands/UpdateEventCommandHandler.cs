using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event.Validators;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Events.Handlers.Commands
{
    public class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand, BaseCommandResponse<Guid>>
    {
        private readonly IEventRepository _eventRepository;
        private readonly IAudienceAgeRepository _audienceAgeRepository;
        private readonly IAudienceGenderRepository _audienceGenderRepository;
        private readonly IEventTypeRepository _eventTypeRepository;
        private readonly IActorRepository _actorRepository;
        private readonly IStorageObjectRepository _storageObjectRepository;
        private readonly IMapper _mapper;

        public UpdateEventCommandHandler(
            IEventRepository eventRepository,
            IAudienceAgeRepository audienceAgeRepository,
            IAudienceGenderRepository audienceGenderRepository,
            IEventTypeRepository eventTypeRepository,
            IActorRepository actorRepository,
            IStorageObjectRepository storageObjectRepository,
            IMapper mapper)
        {
            _eventRepository = eventRepository;
            _audienceAgeRepository = audienceAgeRepository;
            _audienceGenderRepository = audienceGenderRepository;
            _eventTypeRepository = eventTypeRepository;
            _actorRepository = actorRepository;
            _storageObjectRepository = storageObjectRepository;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            var validator = new UpdateEventDtoValidator(_audienceAgeRepository, _audienceGenderRepository, _eventTypeRepository, _actorRepository, _storageObjectRepository);
            var validationResult = await validator.ValidateAsync(request.EventDto);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Event update failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            var @event = await _eventRepository.GetById(request.EventDto.Id);
            if (@event == null)
            {
                response.Success = false;
                response.Message = "Event not found.";
                return response;
            }

            _mapper.Map(request.EventDto, @event);

            await _eventRepository.Update(@event);

            response.Success = true;
            response.Id = @event.Id;
            response.Message = "Event updated successfully.";

            return response;
        }
    }
}

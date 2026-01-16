using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSession.Validators;
using Explore.Application.Features.EventSessions.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.EventSessions.Handlers.Commands
{
    public class UpdateEventSessionCommandHandler : IRequestHandler<UpdateEventSessionCommand, BaseCommandResponse<Guid>>
    {
        private readonly IEventSessionRepository _eventSessionRepository;
        private readonly IEventRepository _eventRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly IRegistrationModeRepository _registrationModeRepository;
        private readonly IMapper _mapper;

        public UpdateEventSessionCommandHandler(
            IEventSessionRepository eventSessionRepository,
            IEventRepository eventRepository,
            ILocationRepository locationRepository,
            IRegistrationModeRepository registrationModeRepository,
            IMapper mapper)
        {
            _eventSessionRepository = eventSessionRepository;
            _eventRepository = eventRepository;
            _locationRepository = locationRepository;
            _registrationModeRepository = registrationModeRepository;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventSessionCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            var validator = new UpdateEventSessionDtoValidator(_eventRepository, _locationRepository, _registrationModeRepository);
            var validationResult = await validator.ValidateAsync(request.EventSessionDto);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Event session update failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            var eventSession = await _eventSessionRepository.GetById(request.EventSessionDto.Id);

            if (eventSession == null)
            {
                response.Success = false;
                response.Message = "Event session not found.";
                return response;
            }

            _mapper.Map(request.EventSessionDto, eventSession);

            await _eventSessionRepository.Update(eventSession);

            response.Success = true;
            response.Id = eventSession.Id;
            response.Message = "Event session updated successfully.";

            return response;
        }
    }
}

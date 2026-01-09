using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionAgendaItem.Validators;
using Explore.Application.Features.EventSessionAgendaItems.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.EventSessionAgendaItems.Handlers.Commands
{
    public class CreateEventSessionAgendaItemCommandHandler : IRequestHandler<CreateEventSessionAgendaItemCommand, BaseCommandResponse<Guid>>
    {
        private readonly IEventSessionAgendaItemRepository _agendaItemRepository;
        private readonly IEventSessionRepository _eventSessionRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly IMapper _mapper;

        public CreateEventSessionAgendaItemCommandHandler(
            IEventSessionAgendaItemRepository agendaItemRepository,
            IEventSessionRepository eventSessionRepository,
            ILocationRepository locationRepository,
            IMapper mapper)
        {
            _agendaItemRepository = agendaItemRepository;
            _eventSessionRepository = eventSessionRepository;
            _locationRepository = locationRepository;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(CreateEventSessionAgendaItemCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            var validator = new CreateEventSessionAgendaItemDtoValidator(_eventSessionRepository, _locationRepository);
            var validationResult = await validator.ValidateAsync(request.AgendaItemDto);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Agenda item creation failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            var agendaItem = _mapper.Map<EventSessionAgendaItem>(request.AgendaItemDto);

            agendaItem = await _agendaItemRepository.Create(agendaItem);

            response.Success = true;
            response.Id = agendaItem.Id;
            response.Message = "Agenda item created successfully.";

            return response;
        }
    }
}

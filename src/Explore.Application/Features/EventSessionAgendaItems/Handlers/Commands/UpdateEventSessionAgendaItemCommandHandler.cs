// ABOUTME: Handler for updating an event session agenda item with validation.
// ABOUTME: Validates input, fetches entity, applies updates.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionAgendaItem.Validators;
using Explore.Application.Features.EventSessionAgendaItems.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionAgendaItems.Handlers.Commands;

public class UpdateEventSessionAgendaItemCommandHandler : IRequestHandler<UpdateEventSessionAgendaItemCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventSessionAgendaItemRepository _agendaItemRepository;
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly IMapper _mapper;

    public UpdateEventSessionAgendaItemCommandHandler(
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

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventSessionAgendaItemCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateEventSessionAgendaItemDtoValidator(_eventSessionRepository, _locationRepository);
        var validationResult = await validator.ValidateAsync(request.AgendaItemDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Agenda item update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var agendaItem = await _agendaItemRepository.GetById(request.AgendaItemDto.Id);

        if (agendaItem == null)
        {
            response.Success = false;
            response.Message = "Agenda item not found.";
            return response;
        }

        _mapper.Map(request.AgendaItemDto, agendaItem);

        await _agendaItemRepository.Update(agendaItem);

        response.Success = true;
        response.Id = agendaItem.Id;
        response.Message = "Agenda item updated successfully.";

        return response;
    }
}

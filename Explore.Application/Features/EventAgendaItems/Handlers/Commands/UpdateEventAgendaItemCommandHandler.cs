// ABOUTME: Handler for updating an existing event-level agenda item with validation and local projection.
// ABOUTME: Validates input, fetches entity, recomputes cached local projections via Reschedule(), re-links EventDayId.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventAgendaItem.Validators;
using Explore.Application.Features.EventAgendaItems.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain.Services.Scheduling;
using MediatR;

namespace Explore.Application.Features.EventAgendaItems.Handlers.Commands;

public class UpdateEventAgendaItemCommandHandler : IRequestHandler<UpdateEventAgendaItemCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventAgendaItemRepository _eventAgendaItemRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IEventDayRepository _eventDayRepository;
    private readonly IEventScheduleProjectionCalculator _scheduleProjectionCalculator;
    private readonly IMapper _mapper;

    public UpdateEventAgendaItemCommandHandler(
        IEventAgendaItemRepository eventAgendaItemRepository,
        IEventRepository eventRepository,
        IEventDayRepository eventDayRepository,
        IEventScheduleProjectionCalculator scheduleProjectionCalculator,
        IMapper mapper)
    {
        _eventAgendaItemRepository = eventAgendaItemRepository;
        _eventRepository = eventRepository;
        _eventDayRepository = eventDayRepository;
        _scheduleProjectionCalculator = scheduleProjectionCalculator;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventAgendaItemCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateEventAgendaItemDtoValidator(_eventRepository);
        var validationResult = await validator.ValidateAsync(request.EventAgendaItemDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event agenda item update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var agendaItem = await _eventAgendaItemRepository.GetById(request.EventAgendaItemDto.Id);
        if (agendaItem == null)
        {
            response.Success = false;
            response.Message = "Event agenda item not found.";
            return response;
        }

        var parentEvent = await _eventRepository.GetById(request.EventAgendaItemDto.EventId);
        if (parentEvent == null || parentEvent.TenantId != agendaItem.TenantId)
        {
            response.Success = false;
            response.Message = "Event does not belong to the same tenant as the agenda item.";
            return response;
        }

        _mapper.Map(request.EventAgendaItemDto, agendaItem);

        agendaItem.Reschedule(
            request.EventAgendaItemDto.StartTime,
            request.EventAgendaItemDto.EndTime,
            parentEvent.EventTimeZoneId ?? parentEvent.Timezone ?? string.Empty,
            _scheduleProjectionCalculator);

        var matchingDay = await _eventDayRepository.FindByEventAndLocalDateAsync(
            parentEvent.Id, agendaItem.LocalStartDate, cancellationToken);
        agendaItem.EventDayId = matchingDay?.Id;

        await _eventAgendaItemRepository.Update(agendaItem);

        response.Success = true;
        response.Id = agendaItem.Id;
        response.Message = "Event agenda item updated successfully.";

        return response;
    }
}

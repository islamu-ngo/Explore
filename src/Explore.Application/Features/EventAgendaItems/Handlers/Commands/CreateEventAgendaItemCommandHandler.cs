// ABOUTME: Handler for creating a new event-level agenda item with validation and local projection.
// ABOUTME: Validates input, maps DTO, computes cached local projections via Reschedule(), auto-links EventDayId.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventAgendaItem.Validators;
using Explore.Application.Features.EventAgendaItems.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Services.Scheduling;
using MediatR;

namespace Explore.Application.Features.EventAgendaItems.Handlers.Commands;

public class CreateEventAgendaItemCommandHandler : IRequestHandler<CreateEventAgendaItemCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventAgendaItemRepository _eventAgendaItemRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IEventDayRepository _eventDayRepository;
    private readonly IEventScheduleProjectionCalculator _scheduleProjectionCalculator;
    private readonly IMapper _mapper;

    public CreateEventAgendaItemCommandHandler(
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

    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventAgendaItemCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new CreateEventAgendaItemDtoValidator(_eventRepository);
        var validationResult = await validator.ValidateAsync(request.EventAgendaItemDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event agenda item creation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var parentEvent = await _eventRepository.GetById(request.EventAgendaItemDto.EventId);
        if (parentEvent == null)
        {
            response.Success = false;
            response.Message = "Event not found in the current tenant.";
            return response;
        }

        var agendaItem = _mapper.Map<EventAgendaItem>(request.EventAgendaItemDto);
        agendaItem.TenantId = parentEvent.TenantId;

        agendaItem.Reschedule(
            request.EventAgendaItemDto.StartTime,
            request.EventAgendaItemDto.EndTime,
            parentEvent.EventTimeZoneId ?? parentEvent.Timezone ?? string.Empty,
            _scheduleProjectionCalculator);

        var matchingDay = await _eventDayRepository.FindByEventAndLocalDateAsync(
            parentEvent.Id, agendaItem.LocalStartDate, cancellationToken);
        agendaItem.EventDayId = matchingDay?.Id;

        agendaItem = await _eventAgendaItemRepository.Create(agendaItem);

        response.Success = true;
        response.Id = agendaItem.Id;
        response.Message = "Event agenda item created successfully.";

        return response;
    }
}

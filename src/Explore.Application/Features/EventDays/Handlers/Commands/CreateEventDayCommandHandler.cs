// ABOUTME: Handler for creating a new EventDay with validation and tenant scoping.
// ABOUTME: Validates event ownership, date uniqueness, maps DTO, sets TenantId from parent event.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventDay.Validators;
using Explore.Application.Features.EventDays.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.EventDays.Handlers.Commands;

public class CreateEventDayCommandHandler : IRequestHandler<CreateEventDayCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventDayRepository _eventDayRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;

    public CreateEventDayCommandHandler(
        IEventDayRepository eventDayRepository,
        IEventRepository eventRepository,
        IMapper mapper)
    {
        _eventDayRepository = eventDayRepository;
        _eventRepository = eventRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventDayCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new CreateEventDayDtoValidator(_eventRepository, _eventDayRepository);
        var validationResult = await validator.ValidateAsync(request.EventDayDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event day creation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var parentEvent = await _eventRepository.GetById(request.EventDayDto.EventId);
        if (parentEvent == null)
        {
            response.Success = false;
            response.Message = "Event not found in the current tenant.";
            return response;
        }

        var eventDay = _mapper.Map<EventDay>(request.EventDayDto);
        eventDay.TenantId = parentEvent.TenantId;

        eventDay = await _eventDayRepository.Create(eventDay);

        response.Success = true;
        response.Id = eventDay.Id;
        response.Message = "Event day created successfully.";

        return response;
    }
}

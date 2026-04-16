// ABOUTME: Handler for updating an existing EventDay with validation.
// ABOUTME: Validates event ownership, date uniqueness (excluding self), applies field updates.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventDay.Validators;
using Explore.Application.Features.EventDays.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventDays.Handlers.Commands;

public class UpdateEventDayCommandHandler : IRequestHandler<UpdateEventDayCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventDayRepository _eventDayRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;

    public UpdateEventDayCommandHandler(
        IEventDayRepository eventDayRepository,
        IEventRepository eventRepository,
        IMapper mapper)
    {
        _eventDayRepository = eventDayRepository;
        _eventRepository = eventRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventDayCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateEventDayDtoValidator(_eventRepository, _eventDayRepository);
        var validationResult = await validator.ValidateAsync(request.EventDayDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event day update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var eventDay = await _eventDayRepository.GetById(request.EventDayDto.Id);
        if (eventDay == null)
        {
            response.Success = false;
            response.Message = "Event day not found.";
            return response;
        }

        var parentEvent = await _eventRepository.GetById(request.EventDayDto.EventId);
        if (parentEvent == null || parentEvent.TenantId != eventDay.TenantId)
        {
            response.Success = false;
            response.Message = "Event does not belong to the same tenant as the event day.";
            return response;
        }

        _mapper.Map(request.EventDayDto, eventDay);

        await _eventDayRepository.Update(eventDay);

        response.Success = true;
        response.Id = eventDay.Id;
        response.Message = "Event day updated successfully.";

        return response;
    }
}

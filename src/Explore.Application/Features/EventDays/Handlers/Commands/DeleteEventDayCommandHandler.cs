// ABOUTME: Handler for soft-deleting an EventDay by Id.
// ABOUTME: Follows the pattern where delete returns BaseCommandResponse<Guid>.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventDays.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventDays.Handlers.Commands;

public class DeleteEventDayCommandHandler : IRequestHandler<DeleteEventDayCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventDayRepository _eventDayRepository;

    public DeleteEventDayCommandHandler(IEventDayRepository eventDayRepository)
    {
        _eventDayRepository = eventDayRepository;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(DeleteEventDayCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var eventDay = await _eventDayRepository.GetById(request.Id);
        if (eventDay == null)
        {
            response.Success = false;
            response.Message = "Event day not found.";
            return response;
        }

        await _eventDayRepository.Delete(eventDay);

        response.Success = true;
        response.Id = eventDay.Id;
        response.Message = "Event day deleted successfully.";

        return response;
    }
}

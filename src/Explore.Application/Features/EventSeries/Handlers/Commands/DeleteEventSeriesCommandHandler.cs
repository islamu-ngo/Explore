// ABOUTME: Handler for soft-deleting an event series.
// ABOUTME: Fetches the entity and delegates to the repository; DbContext converts hard-delete to soft-delete.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventSeries.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSeries.Handlers.Commands;

public class DeleteEventSeriesCommandHandler : IRequestHandler<DeleteEventSeriesCommand, BaseCommandResponse<bool>>
{
    private readonly IEventSeriesRepository _eventSeriesRepository;

    public DeleteEventSeriesCommandHandler(IEventSeriesRepository eventSeriesRepository)
    {
        _eventSeriesRepository = eventSeriesRepository;
    }

    public async Task<BaseCommandResponse<bool>> Handle(DeleteEventSeriesCommand request, CancellationToken cancellationToken)
    {
        var series = await _eventSeriesRepository.GetById(request.Id);
        if (series == null)
        {
            return new BaseCommandResponse<bool>
            {
                Success = false,
                Message = "Event series not found."
            };
        }

        await _eventSeriesRepository.Delete(series);

        return new BaseCommandResponse<bool>
        {
            Id = true,
            Success = true,
            Message = "Event series deleted successfully."
        };
    }
}

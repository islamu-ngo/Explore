using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSeries.Requests.Commands;

public class DeleteEventSeriesCommand : IRequest<BaseCommandResponse<bool>>
{
    public Guid Id { get; set; }
}

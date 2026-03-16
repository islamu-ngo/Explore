using Explore.Application.DTOs.Event;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSeries.Requests.Commands;

public class UpdateEventSeriesCommand : IRequest<BaseCommandResponse<Guid>>
{
    public UpdateEventSeriesDto EventSeriesDto { get; set; } = null!;
}

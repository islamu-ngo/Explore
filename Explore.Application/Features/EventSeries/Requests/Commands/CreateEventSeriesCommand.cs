using Explore.Application.DTOs.Event;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSeries.Requests.Commands;

public class CreateEventSeriesCommand : IRequest<BaseCommandResponse<Guid>>
{
    public CreateEventSeriesDto EventSeriesDto { get; set; } = null!;
}

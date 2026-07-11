// ABOUTME: MediatR command for creating a new event series.
// ABOUTME: Carries the CreateEventSeriesDto payload to CreateEventSeriesCommandHandler.

using Explore.Application.DTOs.EventSeries;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSeries.Requests.Commands;

public class CreateEventSeriesCommand : IRequest<BaseCommandResponse<Guid>>
{
    public CreateEventSeriesDto EventSeriesDto { get; set; } = null!;
}

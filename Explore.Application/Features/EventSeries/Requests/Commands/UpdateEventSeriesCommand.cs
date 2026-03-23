// ABOUTME: MediatR command for updating an existing event series.
// ABOUTME: Carries the UpdateEventSeriesDto payload to UpdateEventSeriesCommandHandler.

using Explore.Application.DTOs.EventSeries;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSeries.Requests.Commands;

public class UpdateEventSeriesCommand : IRequest<BaseCommandResponse<Guid>>
{
    public UpdateEventSeriesDto EventSeriesDto { get; set; } = null!;
}

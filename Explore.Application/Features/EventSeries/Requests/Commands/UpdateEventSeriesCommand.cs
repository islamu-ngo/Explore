// ABOUTME: MediatR command for route-ID EventSeries PATCH updates.
// ABOUTME: Carries If-Match concurrency stamp and grouped update payload.

using Explore.Application.DTOs.EventSeries;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSeries.Requests.Commands;

public class UpdateEventSeriesCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid EventSeriesId { get; set; }
    public Guid ExpectedConcurrencyStamp { get; set; }
    public required UpdateEventSeriesDto EventSeriesDto { get; set; }
}

// ABOUTME: MediatR query for retrieving the top featured event series.
// ABOUTME: Returns the series with the most upcoming events, ordered by view count; null if none.

using Explore.Application.DTOs.EventSeries;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSeries.Requests.Queries;

public class GetTopEventSeriesRequest : IRequest<EventSeriesDto?>
{
}

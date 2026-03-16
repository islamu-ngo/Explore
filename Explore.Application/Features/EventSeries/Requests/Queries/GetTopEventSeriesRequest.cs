using Explore.Application.DTOs.Event;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSeries.Requests.Queries;

public class GetTopEventSeriesRequest : IRequest<EventSeriesDto?>
{
}

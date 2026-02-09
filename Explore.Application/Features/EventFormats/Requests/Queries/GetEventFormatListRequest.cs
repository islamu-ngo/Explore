using System.Collections.Generic;
using Explore.Application.DTOs.EventFormat;
using MediatR;

namespace Explore.Application.Features.EventFormats.Requests.Queries;

public class GetEventFormatListRequest : IRequest<List<EventFormatListDto>>
{
}

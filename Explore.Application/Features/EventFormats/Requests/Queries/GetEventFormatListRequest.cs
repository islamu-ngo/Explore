using Explore.Application.DTOs.EventFormat;
using MediatR;
using System.Collections.Generic;

namespace Explore.Application.Features.EventFormats.Requests.Queries
{
    public class GetEventFormatListRequest : IRequest<List<EventFormatListDto>>
    {
    }
}

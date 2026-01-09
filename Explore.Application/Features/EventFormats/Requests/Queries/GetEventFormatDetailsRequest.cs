using Explore.Application.DTOs.EventFormat;
using MediatR;

namespace Explore.Application.Features.EventFormats.Requests.Queries
{
    public class GetEventFormatDetailsRequest : IRequest<EventFormatDto>
    {
        public int Id { get; set; }
    }
}

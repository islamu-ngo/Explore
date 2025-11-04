using Explore.Application.DTOs.EventType;
using MediatR;

namespace Explore.Application.Features.EventTypes.Requests.Queries
{
    public class GetEventTypeListRequest : IRequest<List<EventTypeListDto>>
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string? Description { get; set; }
    }
}

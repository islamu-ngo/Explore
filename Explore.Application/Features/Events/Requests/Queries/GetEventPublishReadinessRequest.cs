using Explore.Application.DTOs.Event;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Queries;

public class GetEventPublishReadinessRequest : IRequest<EventPublishReadinessDto?>
{
    public Guid Id { get; set; }
}

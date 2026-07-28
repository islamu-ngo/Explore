// ABOUTME: Handles creation of an event capacity pool.
// ABOUTME: Delegates orchestration to the event ticketing service.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventTicketing;

public sealed class CreateEventCapacityPoolCommandHandler(EventTicketingService service) : IRequestHandler<CreateEventCapacityPoolCommand, BaseCommandResponse<Guid>>
{
    public Task<BaseCommandResponse<Guid>> Handle(CreateEventCapacityPoolCommand request, CancellationToken cancellationToken) => service.Handle(request, cancellationToken);
}

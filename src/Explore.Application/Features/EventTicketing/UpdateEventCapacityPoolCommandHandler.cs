// ABOUTME: Handles updates to an event capacity pool.
// ABOUTME: Delegates orchestration to the event ticketing service.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventTicketing;

public sealed class UpdateEventCapacityPoolCommandHandler(EventTicketingService service) : IRequestHandler<UpdateEventCapacityPoolCommand, BaseCommandResponse<Guid>>
{
    public Task<BaseCommandResponse<Guid>> Handle(UpdateEventCapacityPoolCommand request, CancellationToken cancellationToken) => service.Handle(request, cancellationToken);
}

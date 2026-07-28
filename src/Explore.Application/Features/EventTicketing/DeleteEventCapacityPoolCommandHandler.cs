// ABOUTME: Handles removal of an event capacity pool.
// ABOUTME: Delegates orchestration to the event ticketing service.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventTicketing;

public sealed class DeleteEventCapacityPoolCommandHandler(EventTicketingService service) : IRequestHandler<DeleteEventCapacityPoolCommand, BaseCommandResponse<Guid>>
{
    public Task<BaseCommandResponse<Guid>> Handle(DeleteEventCapacityPoolCommand request, CancellationToken cancellationToken) => service.Handle(request, cancellationToken);
}

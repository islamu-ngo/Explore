// ABOUTME: Handles removal of a ticket type from an event ticket catalog draft.
// ABOUTME: Delegates orchestration to the event ticketing service.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventTicketing;

public sealed class DeleteEventTicketTypeCommandHandler(EventTicketingService service) : IRequestHandler<DeleteEventTicketTypeCommand, BaseCommandResponse<Guid>>
{
    public Task<BaseCommandResponse<Guid>> Handle(DeleteEventTicketTypeCommand request, CancellationToken cancellationToken) => service.Handle(request, cancellationToken);
}

// ABOUTME: Handles updating a ticket type in an event ticket catalog draft.
// ABOUTME: Delegates orchestration to the event ticketing service.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventTicketing;

public sealed class UpdateEventTicketTypeCommandHandler(EventTicketingService service) : IRequestHandler<UpdateEventTicketTypeCommand, BaseCommandResponse<Guid>>
{
    public Task<BaseCommandResponse<Guid>> Handle(UpdateEventTicketTypeCommand request, CancellationToken cancellationToken) => service.Handle(request, cancellationToken);
}

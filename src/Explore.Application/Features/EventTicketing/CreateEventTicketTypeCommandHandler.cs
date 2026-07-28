// ABOUTME: Handles adding a ticket type to an event ticket catalog draft.
// ABOUTME: Delegates orchestration to the event ticketing service.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventTicketing;

public sealed class CreateEventTicketTypeCommandHandler(EventTicketingService service) : IRequestHandler<CreateEventTicketTypeCommand, BaseCommandResponse<Guid>>
{
    public Task<BaseCommandResponse<Guid>> Handle(CreateEventTicketTypeCommand request, CancellationToken cancellationToken) => service.Handle(request, cancellationToken);
}

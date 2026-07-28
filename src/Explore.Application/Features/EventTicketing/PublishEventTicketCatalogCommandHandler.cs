// ABOUTME: Handles publishing an event ticket catalog draft.
// ABOUTME: Delegates orchestration to the event ticketing service.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventTicketing;

public sealed class PublishEventTicketCatalogCommandHandler(EventTicketingService service) : IRequestHandler<PublishEventTicketCatalogCommand, BaseCommandResponse<Guid>>
{
    public Task<BaseCommandResponse<Guid>> Handle(PublishEventTicketCatalogCommand request, CancellationToken cancellationToken) => service.Handle(request, cancellationToken);
}

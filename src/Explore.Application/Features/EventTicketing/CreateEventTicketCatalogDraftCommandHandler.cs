// ABOUTME: Handles creation of an event ticket catalog draft.
// ABOUTME: Delegates orchestration to the event ticketing service.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventTicketing;

public sealed class CreateEventTicketCatalogDraftCommandHandler(EventTicketingService service) : IRequestHandler<CreateEventTicketCatalogDraftCommand, BaseCommandResponse<Guid>>
{
    public Task<BaseCommandResponse<Guid>> Handle(CreateEventTicketCatalogDraftCommand request, CancellationToken cancellationToken) => service.Handle(request, cancellationToken);
}

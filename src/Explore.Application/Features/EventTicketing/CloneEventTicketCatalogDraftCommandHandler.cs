// ABOUTME: Handles cloning a published event ticket catalog to a draft.
// ABOUTME: Delegates orchestration to the event ticketing service.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventTicketing;

public sealed class CloneEventTicketCatalogDraftCommandHandler(EventTicketingService service) : IRequestHandler<CloneEventTicketCatalogDraftCommand, BaseCommandResponse<Guid>>
{
    public Task<BaseCommandResponse<Guid>> Handle(CloneEventTicketCatalogDraftCommand request, CancellationToken cancellationToken) => service.Handle(request, cancellationToken);
}

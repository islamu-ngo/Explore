// ABOUTME: Handles ticket catalog management reads for an event.
// ABOUTME: Delegates orchestration to the event ticketing service.

using MediatR;

namespace Explore.Application.Features.EventTicketing;

public sealed class GetEventTicketCatalogManagementQueryHandler(EventTicketingService service) : IRequestHandler<GetEventTicketCatalogManagementQuery, EventTicketCatalogManagementDto?>
{
    public Task<EventTicketCatalogManagementDto?> Handle(GetEventTicketCatalogManagementQuery request, CancellationToken cancellationToken) => service.Handle(request, cancellationToken);
}

// ABOUTME: Defines server-owned resolution of the order quantity governed by a purchase reservation.
// ABOUTME: Prevents callers from understating ticket quantity while keeping persistence entities behind a service.

namespace Explore.Application.Contracts.Services;

public interface ITicketPurchaseOrderResolver
{
    Task<TicketPurchaseOrderSnapshot?> ResolveAsync(
        Guid tenantId,
        Guid eventId,
        Guid orderId,
        CancellationToken cancellationToken);
}

public sealed record TicketPurchaseOrderSnapshot(
    Guid OrderId,
    int Quantity);
